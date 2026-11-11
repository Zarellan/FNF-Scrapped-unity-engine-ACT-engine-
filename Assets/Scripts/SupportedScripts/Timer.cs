using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Timer
{
    public delegate void PrintDelegate();


    public delegate void PrintDelegate2<T>(T name);

    public static IEnumerator timer(int time,PrintDelegate printDelegate) // duration, delegate
    {
        yield return new WaitForSeconds(time);

        printDelegate();
    }

    public static IEnumerator timer(float time,PrintDelegate printDelegate) // duration, delegate
    {
        yield return new WaitForSeconds(time);

        printDelegate();
    }

    public static IEnumerator timer<T>(float time,PrintDelegate2<T> PrintDelegate2,T value) // duration, delegate, parameter value
    {
        yield return new WaitForSeconds(time);

        PrintDelegate2(value);
    }



}
