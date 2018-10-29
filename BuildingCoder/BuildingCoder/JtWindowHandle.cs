#region Namespaces
using System;
using System.Diagnostics;
using System.Windows.Forms;
#endregion

namespace BuildingCoder
{
  /// <summary>
  /// Wrapper class for converting 
  /// IntPtr to IWin32Window.
  /// 
  /// This class is no longer needed as of the introduction 
  /// of the UIApplication MainWindowhandle in Revit 2019.
  /// </summary>
  public class JtWindowHandle : IWin32Window
  {
    IntPtr _hwnd;

    public JtWindowHandle( IntPtr h )
    {
      Debug.Assert( IntPtr.Zero != h,
        "expected non-null window handle" );

      _hwnd = h;
    }

    public IntPtr Handle
    {
      get
      {
        return _hwnd;
      }
    }
  }
}
