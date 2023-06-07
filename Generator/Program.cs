using System;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Security;

Console.OutputEncoding = Encoding.Unicode;
Console.InputEncoding = Encoding.Unicode;


MemoryMappedFile? data = null;
var minNumbers = 20;
var maxNumbers = 30;
var minValue = 10;
var maxValue = 100;
Mutex mutex;

try
{
    mutex = new Mutex(false, @"Global/KalinovksyiDenys");
} catch (WaitHandleCannotBeOpenedException ex)
{
    Debug.WriteLine(ex.Message);
    Console.WriteLine("Неможливо створити м'ютекс");
    return ex.HResult;
}


while (true)
{
    Console.WriteLine("Натисніть пробіл аби створити новий набір значень");
    var consoleKey = Console.ReadKey();

    if (consoleKey.Key == ConsoleKey.Spacebar)
    {
        try
        {
            Generate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Виникла помилка при заповненні масиву: {ex.Message}");
        }
    }
}


void Generate()
{

    if (mutex != null)
    {
        Random random = new Random();
        List<int> numbers = new List<int>();

        var n = random.Next(minNumbers, maxNumbers);
        for (int i = 0; i < n; i++)
        {
            numbers.Add(random.Next(minValue, maxValue));
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

        if (data == null)
            try
            {
                data = MemoryMappedFile.CreateFromFile(
                    @"data.dat",
                    FileMode.Create, "Data", 4 + 4 * maxNumbers, MemoryMappedFileAccess.ReadWrite);
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.Message);
                var new_ex = new Exception("Неможливо створити файл");
                mutex.ReleaseMutex();
                throw new_ex;
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine(ex.Message);
                var new_ex = new Exception("Недостатньо повноважень для створення файла");
                mutex.ReleaseMutex();
                throw new_ex;
            }
        else
            try
            {
                data = MemoryMappedFile.OpenExisting("Data", MemoryMappedFileRights.ReadWrite);
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine(ex.Message);
            }

        using (var view = data.CreateViewAccessor(0, n * 4 + 4))
        {
            view.Write(0, n);
            view.WriteArray<int>(4, numbers.ToArray(), 0, numbers.Count);
        }
        mutex.ReleaseMutex();

        Console.WriteLine($"Нові значення: {string.Join(",", numbers)}");
    }
}
