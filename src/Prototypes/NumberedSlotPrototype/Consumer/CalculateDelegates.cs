namespace NumberedSlotPrototype.Consumer;

// Delegates for an overloaded non-void Calculate method:
//   int Calculate(int x)
//   int Calculate(int x, int y)
public delegate int CalculateDelegate1(int x);
public delegate int CalculateDelegate2(int x, int y);
