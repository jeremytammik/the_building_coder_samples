using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BuildingCoder
{
  public partial class CmdLinkedFileElementsForm : Form
  {
    public CmdLinkedFileElementsForm(
      List<ElementData> a )
    {
      InitializeComponent();
      dataGridView1.DataSource = a;
    }
  }
}