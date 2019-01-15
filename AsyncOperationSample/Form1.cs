using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncOperationSample
{
    public partial class Form1 : Form
    {
        private Worker worker = new Worker();
        private Guid taskId;

        public Form1()
        {
            InitializeComponent();

            this.worker.Completed +=
                new WorkCompletedEventHandler(worker_Completed);
            this.worker.ProgressChanged +=
                new ProgressChangedEventHandler(worker_ProgressChanged);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            System.Console.WriteLine($"button1_Click ({tid})");
            taskId = Guid.NewGuid();
            this.worker.RunAsync(taskId);
        }

        private void worker_Completed(object sender, WorkCompletedEventArgs e)
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            System.Console.WriteLine($"worker_Completed ({tid})");
        }

        private void worker_ProgressChanged(ProgressChangedEventArgs e)
        {
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            System.Console.WriteLine($"worker_ProgressChanged ({tid})");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Console.WriteLine("button2_Clicked");
            worker.CancelAsync(taskId);
        }
    }
}
