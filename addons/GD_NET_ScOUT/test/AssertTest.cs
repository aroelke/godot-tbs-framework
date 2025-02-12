using System;
using System.Collections.Generic;

using Godot;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable MemberCanBeMadeStatic.Local

namespace GD_NET_ScOUT;

[Test]
public partial class AssertTest : Node
{
    [Test]
    private void Fail()
    {
        Assert.Throws<AssertionException>(() => Assert.Fail());
    }

    [Test]
    private void IsTrue()
    {
        Assert.IsTrue(true);
    }

    [Test]
    private void IsFalse()
    {
        Assert.IsFalse(false);
    }

    [Test]
    private void IsNull()
    {
        Assert.IsNull(null);
    }

    [Test]
    private void IsNotNull()
    {
        Assert.IsNotNull(1);
    }

    [Test]
    private void AreSame()
    {
        object o = new Boxed<int>(1);
        Assert.AreSame(o, o);
    }

    [Test]
    private void AreNotSame()
    {
        Assert.AreNotSame(new Boxed<int>(1), new Boxed<int>(1));
    }

    [Test]
    private void AreEqual()
    {
        Assert.AreEqual(new Boxed<int>(1), new Boxed<int>(1));
    }

    [Test]
    private void AreNotEqual()
    {
        Assert.AreNotEqual(new Boxed<int>(1), new Boxed<int>(2));
    }

    [Test]
    private void Throws()
    {
        const string message = "Fake Test Failure";
        ApplicationException result =
            Assert.Throws<ApplicationException>(() => ActionThrowsHelper(true, message));
        Assert.AreEqual(message, result.Message);
    }

    [Test]
    private void DoesNotThrow_Action_Any()
    {
        Assert.DoesNotThrow(() => ActionThrowsHelper(false));

        Assert.Throws<AssertionException>(() => Assert.DoesNotThrow(() => ActionThrowsHelper()));
    }

    [Test]
    private void DoesNotThrow_Action_Typed()
    {
        Assert.DoesNotThrow<ApplicationException>(() => ActionThrowsHelper(false));

        Assert.Throws<AssertionException>(
            () => Assert.DoesNotThrow<ApplicationException>(() => ActionThrowsHelper())
        );

        Assert.Throws<ApplicationException>(
            () => Assert.DoesNotThrow<NullReferenceException>(() => ActionThrowsHelper())
        );
    }

    [Test]
    private void DoesNotThrow_Func_Any()
    {
        Boxed<int> value = Assert.DoesNotThrow(() => FuncThrowsHelper(false));
        Assert.AreEqual(value.Value, 1);

        Assert.Throws<AssertionException>(() => Assert.DoesNotThrow(() => FuncThrowsHelper()));
    }

    [Test]
    private void DoesNotThrow_Func_Typed()
    {
        Boxed<int> value =
            Assert.DoesNotThrow<Boxed<int>, ApplicationException>(() => FuncThrowsHelper(false));
        Assert.AreEqual(value.Value, 1);

        Assert.Throws<AssertionException>(
            () => Assert.DoesNotThrow<Boxed<int>, ApplicationException>(() => FuncThrowsHelper())
        );

        Assert.Throws<ApplicationException>(
            () => Assert.DoesNotThrow<Boxed<int>, NullReferenceException>(() => FuncThrowsHelper())
        );
    }

    private void ActionThrowsHelper(bool shouldThrow = true, string? message = null)
    {
        if (shouldThrow)
        {
            throw new ApplicationException(message);
        }
    }

    private Boxed<int> FuncThrowsHelper(bool shouldThrow = true)
    {
        if (shouldThrow)
        {
            throw new ApplicationException();
        }
        return new Boxed<int>(1);
    }

    private class Boxed<T>
    {
        public readonly T Value;

        public Boxed(T value)
        {
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            return this == obj || (obj is Boxed<T> other && Equals(other));
        }

        private bool Equals(Boxed<T> other)
        {
            return Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return Value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);
        }
    }
}
