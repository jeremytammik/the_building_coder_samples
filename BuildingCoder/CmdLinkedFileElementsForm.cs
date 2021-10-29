using System.Collections.Generic;
using System.Windows.Forms;

namespace BuildingCoder
{
    public partial class CmdLinkedFileElementsForm : Form
    {
        public CmdLinkedFileElementsForm(
            List<ElementData> a)
        {
            InitializeComponent();
            dataGridView1.DataSource = a;
        }
    }
}