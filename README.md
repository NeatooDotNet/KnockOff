


I want to create a source generator for use with unit testing. I want to be able to create stubs of
services/dependencies. I want to create a partial class and when I add an interface to it the source
generator implements the interface members explicitly. So if I create a class and add an interface 
there are no compiler errors. 

The explicit interface implementations will serve as intercepts. If I then add to the partial class
a method that is defined in the interface the source generator will recognize this and
call that method within the explicit interface implementation.

Within the explicit interface we will keep track of calls so that we can Validate in the unit
test that the member was called. 

When compared to Moq a key difference is the setup will not be fluent. A partial class will be the setup
and the test will new up an instance of the partial class. This will handle less cases then Moq but will
be more readable and performant.

This is going to be a source generator to nuget package solution. Same structure as RemoteFactory.


Example:

``` csharp


	public interface IUser
	{
		Role Role { get; set; }

		void SomeMethod();
	}

	[KnockOff]
	public partial class UserKnockOff : IUser
	{
		protected void SomeMethod()
		{
			// User defined logic
		}


	}

	// Source generated code
	public partial class UserKnockOff
	{

		public ExecutionInfo ExecutionInfo { get; } = new ExecutionInfo();

		public class ExecutionInfo
		{
			// This is how the unit test will verify the method was called
			// And define callbacks
			public ExecutionDetails Role { get; set; }

			// This is how the unit test will verify the method was called
			// And define callbacks
			public ExecutionDetails SomeMethod { get; set; }

		}


		// Protected - you can only use UserKnockOff thru the interfaces
		protected Role Role { get; set; }

		Role IUser.Role
		{
			get
			{
				// Capture getter access
				// Define a callback
				return this.Role;
			}

			set
			{
				// Capture setter access
				// Define a callback
				this.Role = value;
			}
		}

		
		// Everything is virtual to allow non-generated code 
		protected void _SomeMethod()
		{
			// Capture method access
			// Define a callback

			// Recognize that the user defined the method then call it
			this.SomeMethod();
		}

		void IUser.SomeMethod() => this._SomeMethod();
	}

```

Steps:


- Create the solution structure similar to RemoteFactory
    - Use the same 'create objects then test them' approach.
- Define an attribute "\[KnockOff\]" to be able to identify classes. 
- Create the Source Generator
- Define the Predicate
- Define the Transform
    - Return all of the information needed to do the source generation
    - If this information changes the source generator executes
    - This MUST be serializable so that the source generator doesn't run unneccessarily
- Define ExecutionDetails
- Create the source generator