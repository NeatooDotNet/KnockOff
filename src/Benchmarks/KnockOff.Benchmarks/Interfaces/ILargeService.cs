namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Large interface with 50 methods.
/// Stress test for proxy generation overhead.
/// </summary>
public interface ILargeService
{
    void VoidMethod01();
    void VoidMethod02();
    void VoidMethod03();
    void VoidMethod04();
    void VoidMethod05();
    void VoidMethod06();
    void VoidMethod07();
    void VoidMethod08();
    void VoidMethod09();
    void VoidMethod10();

    void VoidMethodWithParam01(int p);
    void VoidMethodWithParam02(int p);
    void VoidMethodWithParam03(int p);
    void VoidMethodWithParam04(int p);
    void VoidMethodWithParam05(int p);
    void VoidMethodWithParam06(int p);
    void VoidMethodWithParam07(int p);
    void VoidMethodWithParam08(int p);
    void VoidMethodWithParam09(int p);
    void VoidMethodWithParam10(int p);

    int IntMethod01();
    int IntMethod02();
    int IntMethod03();
    int IntMethod04();
    int IntMethod05();
    int IntMethod06();
    int IntMethod07();
    int IntMethod08();
    int IntMethod09();
    int IntMethod10();

    string StringMethod01();
    string StringMethod02();
    string StringMethod03();
    string StringMethod04();
    string StringMethod05();
    string StringMethod06();
    string StringMethod07();
    string StringMethod08();
    string StringMethod09();
    string StringMethod10();

    int IntMethodWithParam01(int p);
    int IntMethodWithParam02(int p);
    int IntMethodWithParam03(int p);
    int IntMethodWithParam04(int p);
    int IntMethodWithParam05(int p);
    int IntMethodWithParam06(int p);
    int IntMethodWithParam07(int p);
    int IntMethodWithParam08(int p);
    int IntMethodWithParam09(int p);
    int IntMethodWithParam10(int p);
}
