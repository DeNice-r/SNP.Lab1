using System;
using System.Text;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Diagnostics;

Console.OutputEncoding = Encoding.Unicode;
Console.InputEncoding = Encoding.Unicode;

Mutex? mutex = null;

while (mutex == null)
    try
    {
        mutex = Mutex.OpenExisting(@"Global/KalinovksyiDenys");
        if (mutex == null)
        {
            throw new WaitHandleCannotBeOpenedException();
        }
    }
    catch (WaitHandleCannotBeOpenedException ex)
    {
        Debug.WriteLine(ex.Message);
        Console.WriteLine("Запустіть Generator");
        Thread.Sleep(1000);
    }

while (true)
{
    Console.WriteLine("Натисніть пробіл аби почати зворотнє сортування");

    ConsoleKeyInfo consoleKey = Console.ReadKey();

    if (consoleKey.Key == ConsoleKey.Spacebar)
    {
        Console.Clear();
        try
        {
            SortFileData();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    Console.Clear();
}


void SortFileData()
{
    int size = 2;
    int[] numbers;
    MemoryMappedFile data;


    try
    {
        data = MemoryMappedFile.OpenExisting("Data", MemoryMappedFileRights.ReadWrite);
    }
    catch (FileNotFoundException ex)
    {
        Debug.WriteLine(ex.Message);
        var new_ex = new Exception("Не знайдено даних для сортування");
        throw new_ex;
    }

    bool mutex_status;
    try
    {
        mutex_status = mutex.WaitOne(1500);
    }
    catch (AbandonedMutexException ex)
    {
        Debug.WriteLine(ex.Message);
        var new_ex = new Exception("М'ютекс назавжди заблоковано іншим процесом");
        throw new_ex;
    }
    if (!mutex_status)
    {
        var new_ex = new Exception("Неможливо заблокувати м'ютекс");
        throw new_ex;
    }


    using (MemoryMappedViewAccessor view = data.CreateViewAccessor(0, 4))
    {
        size = view.ReadInt32(0);
    }
    mutex.ReleaseMutex();

    Console.WriteLine("Сортування...");
    int[] lastNumbers = new int[0];
    Stack<int> stack = new Stack<int>();
    stack.Push(0);
    stack.Push(size - 1);

    while (stack.Count > 0)
    {
        try
        {
            mutex_status = mutex.WaitOne(1500);
        }
        catch (AbandonedMutexException ex)
        {
            Debug.WriteLine(ex.Message);
            var new_ex = new Exception("М'ютекс назавжди заблоковано іншим процесом");
            throw new_ex;
        }
        if (!mutex_status)
        {
            var new_ex = new Exception("Неможливо заблокувати м'ютекс");
            throw new_ex;
        }

        using (MemoryMappedViewAccessor view = data.CreateViewAccessor(0, 4))
        {
            size = view.ReadInt32(0);
        }

        using (MemoryMappedViewAccessor view = data.CreateViewAccessor(4, size * 4))
        {
            numbers = new int[size];
            view.ReadArray<int>(0, numbers, 0, size);

            if (!Enumerable.SequenceEqual<int>(numbers, lastNumbers))
            {
                stack = new Stack<int>();
                stack.Push(0);
                stack.Push(size - 1);
            }

            int end = stack.Pop();
            int start = stack.Pop();

            if (start >= end)
            {
                mutex.ReleaseMutex();
                Thread.Sleep(1000);
                continue;
            }


            int pivotIndex = start;
            var pivotValue = numbers[end];

            for (int i = start; i < end; i++)
            {
                if (pivotValue.CompareTo(numbers[i]) <= 0)
                {
                    Swap(ref numbers[i], ref numbers[pivotIndex]);
                    pivotIndex++;
                }
            }

            Swap(ref numbers[pivotIndex], ref numbers[end]);
            if (pivotIndex > 1)
            {
                stack.Push(start);
                stack.Push(pivotIndex - 1);
            }
            if (pivotIndex + 1 < end)
            {
                stack.Push(pivotIndex + 1);
                stack.Push(end);
            }

            lastNumbers = numbers;
            view.WriteArray<int>(0, numbers, 0, size);
        }
        mutex.ReleaseMutex();
        
        // Оскільки даний алгоритм значно повільніше починає, аби побачити його роботу, надаю йому фору
        // у вигляді зменшеного періоду сну
        Thread.Sleep(100);
    }
}


void Swap(ref int a, ref int b)
{
    var tmp = a;
    a = b;
    b = tmp;
}