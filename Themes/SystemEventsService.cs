namespace Themes;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

public class SystemEventsService : IDisposable
{
    private const uint WM_WININICHANGE = 0x001A;

    private NativeWindow window;
    private readonly BehaviorSubject<bool> appDarkThemeSubject, systemDarkThemeSubject;

    public SystemEventsService()
    {
        window = new NativeWindow(this);

        appDarkThemeSubject = new BehaviorSubject<bool>(ShouldAppsUseDarkMode());
        systemDarkThemeSubject = new BehaviorSubject<bool>(ShouldSystemUseDarkMode());

        AppDarkTheme = appDarkThemeSubject.AsObservable();
        SystemDarkTheme = systemDarkThemeSubject.AsObservable();
    }

    public IObservable<bool> AppDarkTheme { get; }

    public IObservable<bool> SystemDarkTheme { get; }

    public void Dispose()
    {
        window.Dispose();
    }

    private void ProcessMessage(uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WININICHANGE && Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
        {
            appDarkThemeSubject.OnNext(ShouldAppsUseDarkMode());
            systemDarkThemeSubject.OnNext(ShouldSystemUseDarkMode());
            return;
        }
    }

    [DllImport("UxTheme.dll", EntryPoint = "#132", SetLastError = true)]
    static extern bool ShouldAppsUseDarkMode();

    [DllImport("UxTheme.dll", EntryPoint = "#138", SetLastError = true)]
    static extern bool ShouldSystemUseDarkMode();

    private sealed class NativeWindow : IDisposable
    {
        private readonly string windowId;
        private SystemEventsService owner;

        private WNDPROC proc;

        public unsafe NativeWindow(SystemEventsService owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

            windowId = $"class:com.albertakhmetov.themesample";
            proc = OnWindowMessageReceived;

            fixed (char* className = windowId)
            {
                var classInfo = new WNDCLASSW()
                {
                    lpfnWndProc = proc,
                    lpszClassName = new PCWSTR(className),
                };

                PInvoke.RegisterClass(classInfo);

                Hwnd = PInvoke.CreateWindowEx(
                    dwExStyle: 0,
                    lpClassName: windowId,
                    lpWindowName: windowId,
                    dwStyle: 0,
                    X: 0,
                    Y: 0,
                    nWidth: 0,
                    nHeight: 0,
                    hWndParent: new HWND(IntPtr.Zero),
                    hMenu: null,
                    hInstance: null,
                    lpParam: null);
            }
        }

        ~NativeWindow()
        {
            Dispose(false);
        }

        public HWND Hwnd { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (Hwnd != HWND.Null)
            {
                PInvoke.DestroyWindow(hWnd: Hwnd);
                Hwnd = HWND.Null;

                PInvoke.UnregisterClass(
                    lpClassName: windowId,
                    hInstance: null);
            }
        }

        private LRESULT OnWindowMessageReceived(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            owner.ProcessMessage(msg, wParam, lParam);

            return PInvoke.DefWindowProc(
                hWnd: hwnd,
                Msg: msg,
                wParam: wParam,
                lParam: lParam);
        }
    }
}
