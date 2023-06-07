using System;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Diagnostics;

Console.OutputEncoding = Encoding.Unicode;
Console.InputEncoding = Encoding.Unicode;

Mutex? mutex = null;

while(mutex == null)
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
    Console.WriteLine("Натисніть пробіл аби почати сортування");

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

    Console.WriteLine("Сортування");
    int[] lastNumbers = new int[0];
    for (var i = 1; i < size; i++)
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

        using (MemoryMappedViewAccessor view = data.CreateViewAccessor(4, size * 4))
        {
            numbers = new int[size];
            view.ReadArray<int>(0, numbers, 0, size);

            if (!Enumerable.SequenceEqual<int>(numbers, lastNumbers))
                i = 1;


            var value = numbers[i];
            var j = i;
            while ((j > 0) && (numbers[j - 1] > value))
            {
                Swap(ref numbers[j - 1], ref numbers[j]);
                j--;
            }

            numbers[j] = value;

            lastNumbers = numbers;
            view.WriteArray<int>(0, numbers, 0, size);
        }
        using (MemoryMappedViewAccessor view = data.CreateViewAccessor(0, 4))
        {
            size = view.ReadInt32(0);
        }
        mutex.ReleaseMutex();
        Console.SetCursorPosition(10, 0);
        Console.Write(new String('.', 1 + i % 3) + new String(' ', 2 - i % 3));


        Thread.Sleep(1000);
    }
}


void Swap(ref int a, ref int b)
{
    var tmp = a;
    a = b;
    b = tmp;
}