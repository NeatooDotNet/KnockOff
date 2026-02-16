namespace NumberedSlotPrototype.Consumer;

// Delegates for an overloaded void Process method:
//   void Process(string data)
//   void Process(string data, int priority)
//   void Process(string data, int priority, bool async)
public delegate void ProcessDelegate1(string data);
public delegate void ProcessDelegate2(string data, int priority);
public delegate void ProcessDelegate3(string data, int priority, bool @async);
