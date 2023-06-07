using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Viewer
{
    public partial class Form1 : Form
    {
        bool timer_lock = false;
        int[] lastNumbers = new int[0];

        public Form1()
        {
            InitializeComponent();
            timer.Start();
        }

        private void StartChecking(object sender, EventArgs e)
        {
            if (timer_lock)
            {
                return;
            }

            timer_lock = true;
            Mutex mutex;

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
                MessageBox.Show("Запустіть Generator");
                timer_lock = false;
                return;
            }


            MemoryMappedFile data;
            try
            {
                data = MemoryMappedFile.OpenExisting("Data", MemoryMappedFileRights.ReadWrite);
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show("Не знайдено даних для відображення");
                timer_lock = false;
                return;
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
                timer_lock = false;
                throw new_ex;
            }
            if (!mutex_status)
            {
                var new_ex = new Exception("Неможливо заблокувати м'ютекс");
                timer_lock = false;
                throw new_ex;
            }
            int size;
            using (MemoryMappedViewAccessor reader = data.CreateViewAccessor(0, 4))
            {
                size = reader.ReadInt32(0);
            }

            using (MemoryMappedViewAccessor changer = data.CreateViewAccessor(4, size * 4))
            {
                int[] numbers = new int[size];
                changer.ReadArray<int>(0, numbers, 0, size);

                if (!Enumerable.SequenceEqual<int>(numbers, lastNumbers))
                {
                    listBox1.BeginUpdate();
                    listBox1.Items.Clear();
                        
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        listBox1.Items.Add(new String('*', numbers[i]));
                    }
                    listBox1.EndUpdate();
                }
                lastNumbers = numbers;
            }
            data.Dispose();
            mutex.ReleaseMutex();
            timer_lock = false;
        }
    }
}
