using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZETrainer;

/// <summary>
/// V31 gaming overlay controller.
/// Alt+W toggles the trainer above BRZE without activating the trainer window,
/// so Battle Realms keeps foreground focus and its simulation does not pause.
/// Mouse buttons/toggles still work through MA_NOACTIVATE; keyboard focus is
/// intentionally left with the game. NumericUpDown values remain mouse-adjustable.
/// </summary>
internal static class OverlayHotkeyController
{
    const int WM_HOTKEY=0x0312;
    const int WM_MOUSEACTIVATE=0x0021;
    const int MA_NOACTIVATE=3;
    const uint MOD_ALT=0x0001;
    const uint MOD_NOREPEAT=0x4000;
    const uint VK_W=0x57;
    const int HOTKEY_ID=0x42525A31; // "BRZ1"

    const int GWL_EXSTYLE=-20;
    const int WS_EX_TOOLWINDOW=0x00000080;
    const int WS_EX_APPWINDOW=0x00040000;
    const int WS_EX_NOACTIVATE=0x08000000;
    const int SW_HIDE=0;
    const int SW_SHOWNOACTIVATE=4;
    const uint SWP_NOSIZE=0x0001;
    const uint SWP_NOZORDER=0x0004;
    const uint SWP_NOACTIVATE=0x0010;
    const uint SWP_SHOWWINDOW=0x0040;
    static readonly IntPtr HWND_TOPMOST=new(-1);

    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);
    [DllImport("user32.dll",SetLastError=true)] static extern bool UnregisterHotKey(IntPtr hWnd,int id);
    [DllImport("user32.dll",SetLastError=true)] static extern int GetWindowLong(IntPtr hWnd,int nIndex);
    [DllImport("user32.dll",SetLastError=true)] static extern int SetWindowLong(IntPtr hWnd,int nIndex,int dwNewLong);
    [DllImport("user32.dll",SetLastError=true)] static extern bool ShowWindow(IntPtr hWnd,int nCmdShow);
    [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsChild(IntPtr hWndParent,IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd,out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left,Top,Right,Bottom; }

    sealed class Filter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if(m.Msg==WM_HOTKEY&&m.WParam.ToInt32()==HOTKEY_ID)
            {
                Toggle();
                return true;
            }
            if(m.Msg==WM_MOUSEACTIVATE&&overlayVisible&&form!=null)
            {
                IntPtr root=form.Handle;
                if(m.HWnd==root||IsChild(root,m.HWnd))
                {
                    m.Result=(IntPtr)MA_NOACTIVATE;
                    return true;
                }
            }
            return false;
        }
    }

    static readonly Filter filter=new();
    static Form? form;
    static bool installed;
    static bool overlayMode;
    static bool overlayVisible;

    public static void Attach(Form target)
    {
        if(installed)return;
        form=target;
        installed=true;
        Application.AddMessageFilter(filter);
        RegisterHotKey(IntPtr.Zero,HOTKEY_ID,MOD_ALT|MOD_NOREPEAT,VK_W);
        target.FormClosed+=(_,_)=>Shutdown();
    }

    static IntPtr GameWindow()
    {
        try
        {
            return Process.GetProcessesByName("Battle_Realms_F")
                .Select(p=>p.MainWindowHandle)
                .FirstOrDefault(h=>h!=IntPtr.Zero);
        }
        catch{return IntPtr.Zero;}
    }

    static Rectangle TargetBounds(Form f,IntPtr game)
    {
        Rectangle area;
        if(game!=IntPtr.Zero&&GetWindowRect(game,out var r)&&r.Right>r.Left&&r.Bottom>r.Top)
            area=Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom);
        else
            area=Screen.FromHandle(f.Handle).WorkingArea;

        int w=f.Width,h=f.Height;
        int x=area.Left+(area.Width-w)/2;
        int y=area.Top+(area.Height-h)/2;
        var wa=Screen.FromRectangle(area).WorkingArea;
        x=Math.Max(wa.Left,Math.Min(x,wa.Right-w));
        y=Math.Max(wa.Top,Math.Min(y,wa.Bottom-h));
        return new Rectangle(x,y,w,h);
    }

    static void EnterOverlayMode(Form f)
    {
        if(overlayMode)return;
        overlayMode=true;
        f.SuspendLayout();
        f.TopMost=true;
        f.ShowInTaskbar=false;
        f.Opacity=0.93;
        f.FormBorderStyle=FormBorderStyle.None;
        f.ResumeLayout(true);
    }

    static void ApplyNoActivateStyle(Form f)
    {
        IntPtr hwnd=f.Handle;
        int ex=GetWindowLong(hwnd,GWL_EXSTYLE);
        ex|=WS_EX_NOACTIVATE|WS_EX_TOOLWINDOW;
        ex&=~WS_EX_APPWINDOW;
        SetWindowLong(hwnd,GWL_EXSTYLE,ex);
    }

    static void ShowOverlay()
    {
        var f=form;if(f==null||f.IsDisposed)return;
        EnterOverlayMode(f);
        ApplyNoActivateStyle(f);
        IntPtr game=GameWindow();
        Rectangle b=TargetBounds(f,game);
        IntPtr hwnd=f.Handle;
        ShowWindow(hwnd,SW_SHOWNOACTIVATE);
        SetWindowPos(hwnd,HWND_TOPMOST,b.X,b.Y,b.Width,b.Height,SWP_NOACTIVATE|SWP_SHOWWINDOW);
        overlayVisible=true;
        if(game!=IntPtr.Zero)SetForegroundWindow(game);
    }

    static void HideOverlay()
    {
        var f=form;if(f==null||f.IsDisposed)return;
        ShowWindow(f.Handle,SW_HIDE);
        overlayVisible=false;
        IntPtr game=GameWindow();
        if(game!=IntPtr.Zero)SetForegroundWindow(game);
    }

    static void Toggle()
    {
        if(form==null||form.IsDisposed)return;
        if(overlayVisible)HideOverlay();
        else ShowOverlay();
    }

    static void Shutdown()
    {
        if(!installed)return;
        try{UnregisterHotKey(IntPtr.Zero,HOTKEY_ID);}catch{}
        try{Application.RemoveMessageFilter(filter);}catch{}
        installed=false;overlayVisible=false;overlayMode=false;form=null;
    }
}
