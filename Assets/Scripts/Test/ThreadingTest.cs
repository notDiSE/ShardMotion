using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ThreadingTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    ConcurrentQueue<float> results = new ConcurrentQueue<float>();
    
    void Start()
    {
        StartCoroutine(Generator(100000));
    }

    private void Update()
    {
        while (results.TryDequeue(out float value))
        {
            Print(value);
        }
    }

    // Update is called once per frame
    void Gen(int n)
    {
        List<float> values = new List<float>();
        for (int i = 0; i < n; i++)
        {
            values.Add(i);
        }
        Method(values);
    }

    IEnumerator Generator(int n)
    {
        var wait = new WaitForSeconds(1f / 24f);
        while (true)
        {
            
            
            Task.Run(() =>
            {
                Gen(n);
            });
            
            yield return wait;
        }
    }
    


    void Method(List<float> values)
    {
        float sum = 0;
        foreach (var value in values)
        {
            sum+=value;
        }
        results.Enqueue(sum);
    }

    void Print(float value)
    {
        Debug.Log("Value is:" + value);
    }
}
