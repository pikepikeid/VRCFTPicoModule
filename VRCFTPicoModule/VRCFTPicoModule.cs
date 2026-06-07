using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Reflection;
using VRCFaceTracking;
using VRCFTPicoModule.Utils;
using static VRCFTPicoModule.Utils.Localization;

namespace VRCFTPicoModule;

public enum TrackingMode
{
    Test = 0,
    VRCFTPicoModule = 1,
    Extended = 2
}
public class ModuleConfig
{
    public bool DisableEyeTracking { get; set; }
    public bool DisableExpressionTracking { get; set; }

    public float EyeGainX { get; set; } = 1.0f;
    public float EyeGainY { get; set; } = 1.0f;
    public TrackingMode Mode { get; set; } = TrackingMode.VRCFTPicoModule;
}

public class VRCFTPicoModule : ExtTrackingModule
{
    private static readonly int[] Ports = [29765, 29763];
    private UdpClient[] _clients = [];
    private static UdpClient _udpClient = new();
    private static int _port;
    private Updater? _updater;
    private (bool, bool) _trackingAvailable;
    private ModuleConfig _config = new();

    private static readonly string ConfigFilePath =
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "config.ini"
        );

    private volatile bool _shuttingDown;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        Localization.Initialize(CultureInfo.CurrentUICulture.Name);
        Logger.LogInformation(T("start-init"));

        _config = ReadConfiguration();
        _trackingAvailable = (
           !_config.DisableEyeTracking && eyeAvailable,
           !_config.DisableExpressionTracking && expressionAvailable
       );

        var initializationResult = InitializeAsync().GetAwaiter().GetResult();
        UpdateModuleInfo(initializationResult);
        
        return initializationResult;
    }


    private async Task<(bool eyeSuccess, bool expressionSuccess)> InitializeAsync()
    {
        Logger.LogDebug(T("initializing-udp-clients"), string.Join(", ", Ports));

        var portIndex = await ListenOnPorts();
        if (portIndex == -1) return (false, false);

        _port = Ports[portIndex];
        _udpClient = new UdpClient(_port);
        Logger.LogInformation(T("using-port"), _port);

        if (_config.DisableEyeTracking)
            Logger.LogInformation(T("eye-tracking-disabled"));

        if (_config.DisableExpressionTracking)
            Logger.LogInformation(T("expression-tracking-disabled"));

        _updater = _config.Mode switch
        {
            TrackingMode.Test =>
                new TestModeUpdater(
                    _udpClient,
                    Logger,
                    _port == Ports[1],
                    _config),

            TrackingMode.Extended =>
                new ExtendedUpdater(
                    _udpClient,
                    Logger,
                    _port == Ports[1],
                    _config),

            _ =>
                new Updater(
                    _udpClient,
                    Logger,
                    _port == Ports[1],
                    _config)
        };

        return _trackingAvailable;
    }

    private ModuleConfig ReadConfiguration()
    {
        var config = new ModuleConfig();

        try
        {
            Logger.LogInformation(T("config-path"), ConfigFilePath);

            if (!File.Exists(ConfigFilePath))
            {
                Logger.LogInformation(T("config-not-found"));
                return config;
            }

            foreach (var rawLine in File.ReadAllLines(ConfigFilePath))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                var split = line.Split(':', 2);

                if (split.Length != 2)
                    continue;

                var key = split[0].Trim().ToLowerInvariant();
                var value = split[1].Trim().ToLowerInvariant();

                switch (key)
                {
                    case "eye-tracking":
                        config.DisableEyeTracking =
                            value == "disable";
                        break;

                    case "expression-tracking":
                        config.DisableExpressionTracking =
                            value == "disable";
                        break;

                    case "mode":
                        {
                            if (int.TryParse(value, out var modeValue) &&
                                Enum.IsDefined(typeof(TrackingMode), modeValue))
                            {
                                config.Mode = (TrackingMode)modeValue;

                                Logger.LogInformation(
                                    T("tracking-mode-loaded"),
                                    modeValue
                                );
                            }
                            else
                            {
                                Logger.LogWarning(
                                    T("tracking-mode-invalid"),
                                    value
                                );
                            }

                            break;
                        }

                    case "eye_gain":
                        {
                            var parts = value.Split(',');

                            if (parts.Length >= 2 &&
                                float.TryParse(
                                    parts[0],
                                    NumberStyles.Float,
                                    CultureInfo.InvariantCulture,
                                    out var x) &&
                                float.TryParse(
                                    parts[1],
                                    NumberStyles.Float,
                                    CultureInfo.InvariantCulture,
                                    out var y))
                            {
                                config.EyeGainX = x;
                                config.EyeGainY = y;

                                Logger.LogInformation(
                                    T("eye-gain-loaded"),
                                    x,
                                    y
                                );
                            }
                            else
                            {
                                Logger.LogWarning(
                                    T("eye-gain-invalid"),
                                    value
                                );
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, T("config-load-failed"));
        }

        return config;
    }

    private void UpdateModuleInfo((bool eyeSuccess, bool expressionSuccess) initializationResult)
    {
        var moduleProtocol = _port == Ports[1] ? $" [{T("legacy-protocol")}]" : "";
        var moduleTrackingStatus = initializationResult switch
        {
            { eyeSuccess: true, expressionSuccess: true } => T("full-face-tracking"),
            { eyeSuccess: true, expressionSuccess: false } => T("eye-tracking"),
            { eyeSuccess: false, expressionSuccess: true } => T("expression-tracking"),
            _ => ""
        };
        var modeTag = _config.Mode switch
        {
            TrackingMode.Test => $" [{T("test-mode")}]",
            TrackingMode.Extended => $" [{T("extended-mode")}]",
            _ => ""
        };
        ModuleInformation.Name = "VRCFTPicoModule (modified) / " + moduleTrackingStatus + moduleProtocol + modeTag;
        var stream = GetType().Assembly.GetManifestResourceStream("VRCFTPicoModule.Assets.pico.png");
        ModuleInformation.StaticImages = stream != null ? [stream] : ModuleInformation.StaticImages;
    }

    private async Task<int> ListenOnPorts()
    {
        try
        {
            _clients = Ports.Select(port => new UdpClient(port)
            {
                Client = { ReceiveTimeout = 100 }
            }).ToArray();

            var tasks = _clients.Select(client => client.ReceiveAsync()).ToArray();
        
            if (tasks.Length == 0)
            {
                return -1;
            }
        
            var completedTask = await Task.WhenAny(tasks);

            foreach (var client in _clients) client.Dispose();
        
            return Array.IndexOf(tasks, completedTask);
        }
        catch (Exception ex)
        {
            Logger.LogError(T("init-failed"), ex);
        }
    
        return -1;
    }

    public override void Update()
    {
        if (_shuttingDown) return;

        try
        {
            _updater?.Update(Status);
        }
        catch (AggregateException ex) when (ex.InnerException is ObjectDisposedException) { }
        catch (ObjectDisposedException) { }
    }

    public override void Teardown()
    {
        _shuttingDown = true;

        foreach (var client in _clients)
            client.Dispose();
        _udpClient.Dispose();
    }
}