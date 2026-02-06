using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnockOff.Documentation.Samples.Readme;


#region readme-manual-stub-interface
public interface IMyRepo
{
    User? GetUser(int id);
    void Update(User user);
}
#endregion

#region readme-manual-stub-desired
public class MyRepoManualStub(List<User> Users) : IMyRepo
{
    public User? GetUser(int id)
    {
        return Users.Single(u => u.Id == id);
    }

    public void Update(User user)
    {
        Assert.Contains(user, Users);
    }
}
#endregion


#region readme-knockoff-stub
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        return Users.Single(u => u.Id == id);
    }

    protected override void Update_(User user)
    {
        Assert.Contains(user, Users);
    }
}
#endregion

public class UserDomainModel(IMyRepo repo)
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public bool Fetch(int userId)
    {
        var user = repo.GetUser(userId);

        if (user == null) { return false; }

        Id = user.Id;
        Name = user.Name;
        Email = user.Email;
        IsActive = user.IsActive;

        return true;
    }

    public void Update()
    {
        var user = repo.GetUser(this.Id);

        if (user == null) { throw new NotFoundException(); }

        user.Name = this.Name;
        user.Email = this.Email;
        user.IsActive = this.IsActive;

        repo.Update(user);
    }
}


public class UserDomainModelTests
{

    #region readme-nsub-shared-mock
    public static IMyRepo NSubstituteMock(List<User> users)
    {
        var myRepoMock = Substitute.For<IMyRepo>();

        // Setup: configure GetUser to look up from the list based on id
        myRepoMock.GetUser(Arg.Any<int>())
            .Returns(callInfo => users.SingleOrDefault(u => u.Id == callInfo.Arg<int>()));

        // Setup: configure Update to assert user exists in list
        myRepoMock.When(x => x.Update(Arg.Any<User>()))
            .Do(callInfo => Assert.Contains(callInfo.Arg<User>(), users));

        return myRepoMock;
    }
    #endregion


    [Fact]
    public void FetchTest_KnockOff()
    {
        #region readme-knockoff-fetch-test
        var myRepoKO = new MyRepoStub([new User { Id = 1 }, new User { Id = 2 }]);
        var userDomainModel = new UserDomainModel(myRepoKO);

        Assert.True(userDomainModel.Fetch(1));

        // I have Verify on my Stub!
        myRepoKO.GetUser.Verify(Times.Once);
        #endregion
    }

    [Fact]
    public void FetchTest_NSubstitute()
    {
        var myRepoNSub = NSubstituteMock([new User { Id = 1 }, new User { Id = 2 }]);
        var userDomainModel = new UserDomainModel(myRepoNSub);

        Assert.True(userDomainModel.Fetch(1));

        myRepoNSub.Received(1).GetUser(1);
    }

    [Fact]
    public void UpdateTest_KnockOff()
    {
        var myRepoKO = new MyRepoStub([new User { Id = 1 }, new User { Id = 2 }]);
        var userDomainModel = new UserDomainModel(myRepoKO);

        userDomainModel.Fetch(1);
        userDomainModel.Update();

        // I have Verify on my Stub!
        myRepoKO.GetUser.Verify(Times.Exactly(2));
        myRepoKO.Update.Verify(Times.Once);
    }


    [Fact]
    public void UpdateTest_KnockOff_OnCall()
    {
        #region readme-knockoff-oncall-test
        var user = new User { Id = 1 };
        var myRepoKO = new MyRepoStub([user]);
        var userDomainModel = new UserDomainModel(myRepoKO);

        // OnCall overrides the stub methods
        myRepoKO.GetUser.OnCall(id => user).Verifiable();
        myRepoKO.Update.OnCall(u => Assert.Same(u, user)).Verifiable();

        userDomainModel.Fetch(1);
        userDomainModel.Update();

        myRepoKO.Verify();
        #endregion
    }

}