using System;

public interface A
{
    public void M()
    {
        Console.WriteLine("1");
    }
}

class B : A { }

class C
{
    A a = new B();

    void Main()
    {
        a.M();
    }
}