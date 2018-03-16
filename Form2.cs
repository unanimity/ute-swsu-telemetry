using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace Telemetry
{

    




    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

     public void showIMG(String S)
    {
            try
            {
                FileStream stream = new FileStream(S, FileMode.Open, FileAccess.Read);
                pictureBox1.Image = Image.FromStream(stream);
                stream.Close();
            }
            catch

            {
             
            }

        }
    }
}
