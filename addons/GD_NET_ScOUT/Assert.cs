using System;

namespace GD_NET_ScOUT;

/// <summary>
/// <para>A utility class for test assertions. If an assertion fails, an <see cref="T:GD_NET_ScOUT.AssertionException"/> will be thrown, failing the test.</para>
/// </summary>
/// <seealso cref="T:GD_NET_ScOUT.AssertTest"/>
public static class Assert
{
    /// <summary>
    /// <para>Fail unconditionally.</para> 
    /// </summary>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.Fail">AssertTest.Fail()</seealso>
    public static void Fail(string? message = null)
    {
        throw new AssertionException($"Call to Fail(). {message ?? string.Empty}");
    }

    /// <summary>
    /// <para>Fail if <paramref name="value"/> is false.</para> 
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.IsTrue">AssertTest.IsTrue()</seealso>
    public static void IsTrue(bool value, string? message = null)
    {
        if (!value)
        {
            throw new AssertionException($"{value} is not true. {message ?? string.Empty}");
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value"/> is true.</para> 
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.IsFalse">AssertTest.IsFalse()</seealso>
    public static void IsFalse(bool value, string? message = null)
    {
        if (value)
        {
            throw new AssertionException($"{value} is not false. {message ?? string.Empty}");
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value"/> is null.</para> 
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.IsNull">AssertTest.IsNull()</seealso>
    public static void IsNull(object? value, string? message = null)
    {
        if (value is not null)
        {
            throw new AssertionException($"{value} is not null. {message ?? string.Empty}");
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value"/> is not null.</para> 
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.IsNotNull">AssertTest.IsNotNull()</seealso>
    public static void IsNotNull(object? value, string? message = null)
    {
        if (value is null)
        {
            throw new AssertionException($"{value} is null. {message ?? string.Empty}");
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value1"/> and <paramref name="value2"/> are not the same instance.</para>
    /// <para>See <see cref="M:System.Object.ReferenceEquals(System.Object,System.Object)"/>.</para>
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.AreSame">AssertTest.AreSame()</seealso>
    public static void AreSame(object? value1, object? value2, string? message = null)
    {
        if (!ReferenceEquals(value1, value2))
        {
            throw new AssertionException(
                $"{value1} is not the same reference as {value2}. {message ?? string.Empty}"
            );
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value1"/> and <paramref name="value2"/> are the same instance.</para>
    /// <para>See <see cref="M:System.Object.ReferenceEquals(System.Object,System.Object)"/>.</para>
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.AreNotSame">AssertTest.AreNotSame()</seealso>
    public static void AreNotSame(object? value1, object? value2, string? message = null)
    {
        if (ReferenceEquals(value1, value2))
        {
            throw new AssertionException(
                $"{value1} is the same reference as {value2}. {message ?? string.Empty}"
            );
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value1"/> and <paramref name="value2"/> are equal.</para>
    /// <para>See <see cref="M:System.Object.Equals(System.Object,System.Object)"/>.</para>
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.AreEqual">AssertTest.AreEqual()</seealso>
    public static void AreEqual<T>(T? value1, T? value2, string? message = null)
    {
        if (!Equals(value1, value2))
        {
            throw new AssertionException(
                $"{value1} is not equal to {value2}. {message ?? string.Empty}"
            );
        }
    }

    /// <summary>
    /// <para>Fail if <paramref name="value1"/> and <paramref name="value2"/> are not equal.</para>
    /// <para>See <see cref="M:System.Object.Equals(System.Object,System.Object)"/>.</para>
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <param name="message">An optional exception message.</param>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.AreNotEqual">AssertTest.AreNotEqual()</seealso>
    public static void AreNotEqual<T>(T? value1, T? value2, string? message = null)
    {
        if (Equals(value1, value2))
        {
            throw new AssertionException(
                $"{value1} is equal to {value2}. {message ?? string.Empty}"
            );
        }
    }

    /// <summary>
    /// <para>Fail if invoking <paramref name="action"/> does not throw an exception of type <typeparamref name="TE"/>.</para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="message">An optional exception message.</param>
    /// <typeparam name="TE">The exception type.</typeparam>
    /// <returns>The thrown exception.</returns>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.Throws">AssertTest.Throws()</seealso>
    public static TE Throws<TE>(Action action, string? message = null) where TE : Exception
    {
        try
        {
            action.Invoke();
        }
        catch (TE exception)
        {
            return exception;
        }

        throw new AssertionException($"No {nameof(TE)} was thrown. {message ?? string.Empty}");
    }

    /// <summary>
    /// <para>Fail if invoking <paramref name="action"/> throws any exception. Useful for differentiating expected from unexpected exceptions.</para>
    /// <para>Note that an exception being thrown will cause test to be marked as a failure, rather than an error.</para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="message">An optional exception message.</param>
    /// <returns>The result of invoking <paramref name="action"/>.</returns>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.DoesNotThrow_Action_Any">AssertTest.DoesNotThrow_Action_Any()</seealso>
    public static void DoesNotThrow(Action action, string? message = null)
    {
        DoesNotThrow<Exception>(action, message);
    }

    /// <summary>
    /// <para>Fail if invoking <paramref name="action"/> throws an exception of type <typeparamref name="TE"/>. Useful for differentiating expected from unexpected exceptions.</para>
    /// <para>Note that an exception of type <typeparamref name="TE"/> being thrown will cause test to be marked as a failure, rather than an error. Any other exception being thrown will cause test to be marked as an error.</para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="message">An optional exception message.</param>
    /// <typeparam name="TE">The type of <see cref="T:System.Exception"/> that should not be thrown.</typeparam>
    /// <returns>The result of invoking <paramref name="action"/>.</returns>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.DoesNotThrow_Action_Typed">AssertTest.DoesNotThrow_Action_Typed()</seealso>
    public static void DoesNotThrow<TE>(Action action, string? message = null) where TE : Exception
    {
        try
        {
            action.Invoke();
        }
        catch (TE exception)
        {
            throw new AssertionException(
                $"{exception.GetType().Name} was thrown. {message ?? string.Empty}", exception
            );
        }
    }

    /// <summary>
    /// <para>Fail if invoking <paramref name="action"/> throws any exception. Otherwise return invocation result. Useful for differentiating expected from unexpected exceptions.</para>
    /// <para>Note that an exception being thrown will cause test to be marked as a failure, rather than an error.</para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="message">An optional exception message.</param>
    /// <typeparam name="T">The return type of <paramref name="action"/>.</typeparam>
    /// <returns>The result of invoking <paramref name="action"/>.</returns>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.DoesNotThrow_Func_Any">AssertTest.DoesNotThrow_Func_Any()</seealso>
    public static T DoesNotThrow<T>(Func<T> action, string? message = null)
    {
        return DoesNotThrow<T, Exception>(action, message);
    }

    /// <summary>
    /// <para>Fail if invoking <paramref name="action"/> throws an exception of type <typeparamref name="TE"/>. Otherwise return invocation result. Useful for differentiating expected from unexpected exceptions.</para>
    /// <para>Note that an exception of type <typeparamref name="TE"/> being thrown will cause test to be marked as a failure, rather than an error. Any other exception being thrown will cause test to be marked as an error.</para>
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="message">An optional exception message.</param>
    /// <typeparam name="T">The return type of <paramref name="action"/>.</typeparam>
    /// <typeparam name="TE">The type of <see cref="T:System.Exception"/> that should not be thrown.</typeparam>
    /// <returns>The result of invoking <paramref name="action"/>.</returns>
    /// <exception cref="AssertionException">If assertion fails.</exception>
    /// <seealso cref="M:GD_NET_ScOUT.AssertTest.DoesNotThrow_Func_Typed">AssertTest.DoesNotThrow_Func_Typed()</seealso>
    public static T DoesNotThrow<T, TE>(Func<T> action, string? message = null) where TE : Exception
    {
        try
        {
            return action.Invoke();
        }
        catch (TE exception)
        {
            throw new AssertionException(
                $"{exception.GetType().Name} was thrown. {message ?? string.Empty}", exception
            );
        }
    }
}

public class AssertionException : Exception
{
    public AssertionException(string? message) : base(message ?? string.Empty) {}

    public AssertionException(string? message, Exception? innerException) : base(
        message ?? string.Empty, innerException
    ) {}
}
