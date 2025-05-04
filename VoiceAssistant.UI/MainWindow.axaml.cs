using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.IO;
using VoiceAssistant.UI.ViewModels;
using VoiceAssistant.UI.Views;

namespace VoiceAssistant.UI;

public partial class MainWindow : Window
{
    private TrayIcon? _trayIcon;
    private MainWindowViewModel? _viewModel;
    private bool _closing;

    public MainWindow()
    {
        InitializeComponent();
        Content = new MainView();
        
        // Handle minimize event
        this.DataContextChanged += OnDataContextChanged;
        
        // Set up the tray icon
        SetupTrayIcon();
        
        // Handle closing
        this.Closing += OnWindowClosing;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.MinimizeToTrayRequested += OnMinimizeToTrayRequested;
        }
    }
    
    private void OnMinimizeToTrayRequested(object? sender, bool e)
    {
        MinimizeToTray();
    }
    
    private void SetupTrayIcon()
    {
        try
        {
            var menu = new NativeMenu();
            var showItem = new NativeMenuItem("Show");
            showItem.Click += ShowWindowFromTray;
            menu.Add(showItem);
            
            menu.Add(new NativeMenuItemSeparator());
            
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += ExitApplication;
            menu.Add(exitItem);
            
            // Try multiple paths to find the icon
            WindowIcon? icon = null;
            
            // Possible paths for the icon file
            string[] possiblePaths = new[]
            {
                // Current directory
                "Assets/app-icon.png",
                // Relative to executing assembly path
                Path.Combine(AppContext.BaseDirectory, "Assets", "app-icon.png"),
                // Absolute path in project
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app-icon.png"),
                // Fallback to application base directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-icon.png")
            };
            
            // Try each path until we find the icon
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        icon = new WindowIcon(path);
                        Console.WriteLine($"Found tray icon at: {path}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading icon from {path}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Icon not found at path: {path}");
                }
            }
            
            // If no icon found, create a simple fallback
            if (icon == null)
            {
                try
                {
                    // Create a simple bitmap with a microphone icon
                    var fallbackIcon = CreateFallbackIcon();
                    icon = new WindowIcon(fallbackIcon);
                    Console.WriteLine("Using fallback icon");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating fallback icon: {ex.Message}");
                }
            }
            
            // Create and show the tray icon
            _trayIcon = new TrayIcon
            {
                Icon = icon, // Might be null but that's OK
                ToolTipText = "Voice Assistant",
                Menu = menu,
                IsVisible = false // Start hidden until MinimizeToTray is called
            };
            
            _trayIcon.Clicked += ShowWindowFromTray;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create tray icon: {ex.Message}");
        }
    }
    
    private Bitmap CreateFallbackIcon()
    {
        // Create a 32x32 bitmap for the icon
        var bitmap = new WriteableBitmap(new PixelSize(32, 32), new Vector(96, 96), PixelFormat.Bgra8888);
        
        using (var context = bitmap.Lock())
        {
            // Fill with a blue background
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    unsafe
                    {
                        byte* pixel = (byte*)context.Address + y * context.RowBytes + x * 4;
                        // BGRA format: blue (0-255)
                        pixel[0] = 200; // B
                        pixel[1] = 120; // G
                        pixel[2] = 50;  // R
                        pixel[3] = 255; // A (opacity)
                    }
                }
            }
        }
        
        return bitmap;
    }
    
    private void MinimizeToTray()
    {
        if (_trayIcon != null)
        {
            // Show the tray icon
            _trayIcon.IsVisible = true;
            
            // Hide the window
            this.Hide();
        }
    }
    
    private void ShowWindowFromTray(object? sender, EventArgs e)
    {
        // Hide the tray icon
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
        }
        
        // Show the window
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }
    
    private void ExitApplication(object? sender, EventArgs e)
    {
        _closing = true;
        Close();
    }
    
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_closing)
        {
            e.Cancel = true;
            MinimizeToTray();
        }
        else
        {
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            
            if (_viewModel != null)
            {
                _viewModel.MinimizeToTrayRequested -= OnMinimizeToTrayRequested;
            }
        }
    }
}