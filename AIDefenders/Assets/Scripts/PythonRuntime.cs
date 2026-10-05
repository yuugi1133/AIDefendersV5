using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class PythonRuntime
{
    public const int RequiredMajor = 3;
    public const int RequiredMinor = 11;
    public const string DownloadPage = "https://www.python.org/downloads/release/python-3119/";

    public static string RequiredVersionLabel
    {
        get { return RequiredMajor + "." + RequiredMinor; }
    }

    public class TorchInstallPlan
    {
        public string GpuName = "";
        public string DriverVersion = "";
        public string ComputeCap = "";
        public bool UseCuda;
        public string CudaTag = "cpu";
        public string IndexUrl = "https://download.pytorch.org/whl/cpu";
        public string Message = "";
    }

    public static TorchInstallPlan DetectTorchInstall()
    {
        var plan = new TorchInstallPlan();
        plan.UseCuda = false;
        plan.CudaTag = "cpu";
        plan.IndexUrl = "https://download.pytorch.org/whl/cpu";
        plan.Message = "NVIDIA GPU를 찾지 못해 CPU용 PyTorch를 설치합니다.";

        string name;
        string driver;
        string compute;
        if (!TryQueryNvidiaGpu(out name, out driver, out compute))
            return plan;

        plan.GpuName = name;
        plan.DriverVersion = driver;
        plan.ComputeCap = compute;

        int driverMajor = ParseLeadingInt(driver);
        float computeCap = ParseComputeCap(compute);

        if (computeCap > 0f && computeCap < 5.0f)
        {
            plan.Message = name + " (compute " + compute + ")은 현재 CUDA PyTorch를 지원하지 않아 CPU용을 설치합니다.";
            return plan;
        }

        if (driverMajor >= 560 && (computeCap <= 0f || computeCap >= 5.0f))
        {
            plan.UseCuda = true;
            plan.CudaTag = "cu126";
            plan.IndexUrl = "https://download.pytorch.org/whl/cu126";
            plan.Message = name + " / driver " + driver + " → CUDA 12.6 PyTorch";
            return plan;
        }

        if (driverMajor >= 551 && (computeCap <= 0f || computeCap >= 5.0f))
        {
            plan.UseCuda = true;
            plan.CudaTag = "cu124";
            plan.IndexUrl = "https://download.pytorch.org/whl/cu124";
            plan.Message = name + " / driver " + driver + " → CUDA 12.4 PyTorch";
            return plan;
        }

        if (driverMajor >= 452 && (computeCap <= 0f || computeCap >= 3.7f))
        {
            plan.UseCuda = true;
            plan.CudaTag = "cu118";
            plan.IndexUrl = "https://download.pytorch.org/whl/cu118";
            plan.Message = name + " / driver " + driver + " → CUDA 11.8 PyTorch";
            return plan;
        }

        plan.Message = name + " 드라이버(" + driver + ")가 낮아 CPU용 PyTorch를 설치합니다. NVIDIA 드라이버를 업데이트하면 GPU 학습이 가능합니다.";
        return plan;
    }

    public static bool IsTorchCudaReady()
    {
        string torchLib = Path.Combine(GetVenvDir(), "Lib", "site-packages", "torch", "lib");
        if (!Directory.Exists(torchLib))
            return false;

        string[] cudaRuntime = Directory.GetFiles(torchLib, "cudart*.dll");
        return cudaRuntime != null && cudaRuntime.Length > 0;
    }

    static bool TryQueryNvidiaGpu(out string name, out string driver, out string compute)
    {
        name = "";
        driver = "";
        compute = "";

        string[] smiPaths =
        {
            "nvidia-smi",
            Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe")
        };

        string output = "";
        bool ran = false;
        for (int i = 0; i < smiPaths.Length; i++)
        {
            if (TryRun(smiPaths[i], "--query-gpu=name,driver_version,compute_cap --format=csv,noheader", out output, 8000))
            {
                ran = true;
                break;
            }
        }

        if (!ran || string.IsNullOrEmpty(output))
            return false;

        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        float bestCompute = -1f;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("NVIDIA-SMI", StringComparison.OrdinalIgnoreCase) ||
                line.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2)
                continue;

            string gpuName = parts[0].Trim();
            string gpuDriver = parts[1].Trim();
            string gpuCompute = parts.Length > 2 ? parts[2].Trim() : "";
            float cap = ParseComputeCap(gpuCompute);
            if (string.IsNullOrEmpty(name) || cap > bestCompute)
            {
                name = gpuName;
                driver = gpuDriver;
                compute = gpuCompute;
                bestCompute = cap;
            }
        }

        return !string.IsNullOrEmpty(name);
    }

    static int ParseLeadingInt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int end = 0;
        while (end < text.Length && char.IsDigit(text[end]))
            end++;
        if (end == 0)
            return 0;

        int value;
        return int.TryParse(text.Substring(0, end), out value) ? value : 0;
    }

    static float ParseComputeCap(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        float value;
        return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0f;
    }


    public const string ServerHost = "127.0.0.1";
    public const int ServerPort = 8000;

    public class TrainedModelInfo
    {
        public string DatasetName = "";
        public string WeightsPath = "";
        public string WeightsFile = "";
    }

    public static string GetPythonServerDir()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Python HTTP Server"));
    }

    public static string GetDatasetsRoot()
    {
        return Path.Combine(Application.dataPath, "Datasets");
    }

    public static string GetServerHealthUrl()
    {
        return "http://" + ServerHost + ":" + ServerPort.ToString(CultureInfo.InvariantCulture) + "/health";
    }

    public static void ListTrainedModels(List<TrainedModelInfo> results)
    {
        if (results == null)
            return;

        results.Clear();
        string root = GetDatasetsRoot();
        if (!Directory.Exists(root))
            return;

        string[] dirs = Directory.GetDirectories(root);
        Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < dirs.Length; i++)
        {
            string weightsPath = FindDatasetWeights(dirs[i]);
            if (string.IsNullOrEmpty(weightsPath))
                continue;

            var info = new TrainedModelInfo();
            info.DatasetName = Path.GetFileName(dirs[i]);
            info.WeightsPath = weightsPath;
            info.WeightsFile = Path.GetFileName(weightsPath);
            results.Add(info);
        }
    }

    public static string FindDatasetWeights(string datasetFolder)
    {
        if (string.IsNullOrEmpty(datasetFolder) || !Directory.Exists(datasetFolder))
            return "";

        string weightsDir = Path.Combine(datasetFolder, "weights");
        string best = Path.Combine(weightsDir, "best.pt");
        if (File.Exists(best))
            return Path.GetFullPath(best);

        string last = Path.Combine(weightsDir, "last.pt");
        if (File.Exists(last))
            return Path.GetFullPath(last);

        return "";
    }

    public static bool TryStartYoloServer(string weightsPath, out Process process, out string error)
    {
        process = null;
        error = "";

        if (!IsVenvReady())
        {
            error = "Python " + RequiredVersionLabel + " 가상환경(.venv)이 아직 준비되지 않았습니다.\n메인 메뉴에서 라이브러리를 설치하세요.";
            return false;
        }

        if (string.IsNullOrEmpty(weightsPath) || !File.Exists(weightsPath))
        {
            error = "YOLO 가중치 파일을 찾을 수 없습니다:\n" + weightsPath;
            return false;
        }

        string serverDir = GetPythonServerDir();
        string scriptPath = Path.Combine(serverDir, "server.py");
        string venvPython = GetVenvPython();
        if (!File.Exists(scriptPath))
        {
            error = "server.py를 찾을 수 없습니다:\n" + scriptPath;
            return false;
        }

        string batPath = Path.Combine(serverDir, "_start_server.bat");
        try
        {
            WriteTextUtf8(batPath, BuildYoloServerBat(serverDir, venvPython, scriptPath, weightsPath));
        }
        catch (Exception e)
        {
            error = "서버 실행 스크립트를 만들지 못했습니다.\n" + e.Message;
            return false;
        }

        return TryStartVisibleBat(serverDir, batPath, out process, out error);
    }

    public static bool TryFindRequiredPython(out string pythonExe, out string versionText)
    {
        pythonExe = "";
        versionText = "";

        if (TryPyLauncher(out pythonExe, out versionText))
            return true;

        string[] commonPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python311", "python.exe"),
            @"C:\Python311\python.exe"
        };

        for (int i = 0; i < commonPaths.Length; i++)
        {
            if (IsRequiredPython(commonPaths[i], out versionText))
            {
                pythonExe = commonPaths[i];
                return true;
            }
        }

        if (TryCommand("python", "--version", out pythonExe, out versionText) && IsRequiredVersion(versionText))
            return true;

        pythonExe = "";
        versionText = "";
        return false;
    }

    static bool TryPyLauncher(out string pythonExe, out string versionText)
    {
        pythonExe = "";
        versionText = "";

        string output;
        if (!TryRun("py", "-" + RequiredVersionLabel + " -c \"import sys; print(sys.executable); print(sys.version)\"", out output))
            return false;

        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return false;

        string exe = lines[0].Trim().Trim('"');
        string version = lines.Length > 1 ? lines[1].Trim() : "";
        if (!IsRequiredPython(exe, out version))
            return false;

        pythonExe = exe;
        versionText = version;
        return true;
    }

    static bool TryCommand(string command, string args, out string executable, out string versionText)
    {
        executable = command;
        versionText = "";
        string output;
        if (!TryRun(command, args, out output))
            return false;

        versionText = output.Trim();
        return IsRequiredVersion(versionText);
    }

    public static bool IsRequiredPython(string pythonExe, out string versionText)
    {
        versionText = "";
        if (string.IsNullOrEmpty(pythonExe) || !File.Exists(pythonExe))
            return false;

        string output;
        if (!TryRun(pythonExe, "-c \"import sys; print(str(sys.version_info[0])+'.'+str(sys.version_info[1])+'.'+str(sys.version_info[2]))\"", out output))
            return false;

        versionText = output.Trim();
        return IsRequiredVersion(versionText);
    }

    public static bool IsRequiredVersion(string versionText)
    {
        if (string.IsNullOrEmpty(versionText))
            return false;

        string text = versionText.Replace("Python", "").Trim();
        string[] parts = text.Split(new[] { ' ', '.', '+', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        int major;
        int minor;
        if (!int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor))
            return false;

        return major == RequiredMajor && minor == RequiredMinor;
    }

    public static void OpenOfficialDownloadPage()
    {
        Application.OpenURL(DownloadPage);
    }

    static bool TryRun(string fileName, string arguments, out string output)
    {
        return TryRun(fileName, arguments, out output, 5000);
    }

    static bool TryRun(string fileName, string arguments, out string output, int timeoutMs)
    {
        output = "";
        try
        {
            var info = new ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = arguments;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            Process process = Process.Start(info);
            if (process == null)
                return false;

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(); } catch (Exception) { }
                process.Dispose();
                return false;
            }

            process.Dispose();
            output = (stdout + " " + stderr).Trim();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string GetVenvDir()
    {
        return Path.Combine(GetPythonServerDir(), ".venv");
    }

    public static string GetVenvPython()
    {
        return Path.Combine(GetVenvDir(), "Scripts", "python.exe");
    }

    public static string GetRequirementsPath()
    {
        return Path.Combine(GetPythonServerDir(), "requirements.txt");
    }

    public static bool IsVenvReady()
    {
        if (!File.Exists(GetVenvPython()))
            return false;

        string sitePackages = Path.Combine(GetVenvDir(), "Lib", "site-packages");
        return Directory.Exists(Path.Combine(sitePackages, "ultralytics")) &&
            Directory.Exists(Path.Combine(sitePackages, "fastapi")) &&
            Directory.Exists(Path.Combine(sitePackages, "cv2"));
    }

    public static bool TryStartLibrarySetup(string systemPython, out Process process, out string error)
    {
        process = null;
        error = "";

        string serverDir = GetPythonServerDir();
        string requirements = GetRequirementsPath();
        if (!Directory.Exists(serverDir))
        {
            error = "Python HTTP Server 폴더를 찾을 수 없습니다:\n" + serverDir;
            return false;
        }

        if (!File.Exists(requirements))
        {
            error = "requirements.txt를 찾을 수 없습니다:\n" + requirements;
            return false;
        }

        if (string.IsNullOrEmpty(systemPython) || !File.Exists(systemPython))
        {
            error = "Python " + RequiredVersionLabel + " 실행 파일을 찾을 수 없습니다.";
            return false;
        }

        string venvDir = GetVenvDir();
        string venvPython = GetVenvPython();
        string batPath = Path.Combine(serverDir, "_install_venv.bat");
        TorchInstallPlan torchPlan = DetectTorchInstall();
        UnityEngine.Debug.Log("PyTorch 설치 계획: " + torchPlan.Message);

        try
        {
            WriteTextUtf8(batPath, BuildLibrarySetupBat(serverDir, systemPython, venvDir, venvPython, requirements, torchPlan));
        }
        catch (Exception e)
        {
            error = "설치 스크립트를 만들지 못했습니다.\n" + e.Message;
            return false;
        }

        return TryStartVisibleBat(serverDir, batPath, out process, out error);
    }

    public static bool TryStartYoloTraining(
        string dataYaml,
        int epochs,
        int batch,
        string datasetName,
        string resultPath,
        string weightsDir,
        out Process process,
        out string error)
    {
        process = null;
        error = "";

        if (!IsVenvReady())
        {
            error = "Python " + RequiredVersionLabel + " 가상환경(.venv)이 아직 준비되지 않았습니다.\n메인 메뉴에서 라이브러리를 설치하세요.";
            return false;
        }

        string serverDir = GetPythonServerDir();
        string scriptPath = Path.Combine(serverDir, "yolo_train.py");
        string venvPython = GetVenvPython();
        if (!File.Exists(scriptPath))
        {
            error = "yolo_train.py를 찾을 수 없습니다:\n" + scriptPath;
            return false;
        }

        string batPath = Path.Combine(serverDir, "_train_yolo.bat");
        try
        {
            WriteTextUtf8(batPath, BuildYoloTrainBat(
                serverDir,
                venvPython,
                scriptPath,
                dataYaml,
                epochs,
                batch,
                datasetName,
                resultPath,
                weightsDir));
        }
        catch (Exception e)
        {
            error = "학습 스크립트를 만들지 못했습니다.\n" + e.Message;
            return false;
        }

        return TryStartVisibleBat(serverDir, batPath, out process, out error);
    }

    static bool TryStartVisibleBat(string workingDir, string batPath, out Process process, out string error)
    {
        process = null;
        error = "";
        try
        {
            var info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.Arguments = "/D /C call " + Quote(batPath);
            info.WorkingDirectory = workingDir;
            info.UseShellExecute = true;
            info.WindowStyle = ProcessWindowStyle.Normal;
            process = Process.Start(info);
            if (process == null)
            {
                error = "CMD 창을 시작하지 못했습니다.";
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    static string BuildLibrarySetupBat(string serverDir, string systemPython, string venvDir, string venvPython, string requirements, TorchInstallPlan torchPlan)
    {
        if (torchPlan == null)
            torchPlan = DetectTorchInstall();

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal");
        sb.AppendLine("cd /d " + Quote(serverDir));
        sb.AppendLine("title YOLO library setup");
        sb.AppendLine("echo Python " + RequiredVersionLabel + " virtualenv / library setup");
        sb.AppendLine("echo GPU: " + SanitizeCmdText(string.IsNullOrEmpty(torchPlan.GpuName) ? "not detected" : torchPlan.GpuName));
        sb.AppendLine("echo Torch: " + SanitizeCmdText(torchPlan.UseCuda ? torchPlan.CudaTag : "cpu"));
        sb.AppendLine("echo.");
        sb.AppendLine("if exist " + Quote(venvPython) + " goto :pip");
        sb.AppendLine("echo Creating virtualenv...");
        sb.AppendLine(Quote(systemPython) + " -m venv " + Quote(venvDir));
        sb.AppendLine("if errorlevel 1 goto :fail");
        sb.AppendLine(":pip");
        sb.AppendLine("echo Upgrading pip...");
        sb.AppendLine(Quote(venvPython) + " -m pip install --upgrade pip");
        sb.AppendLine("if errorlevel 1 goto :fail");
        sb.AppendLine("echo Removing previous torch if present...");
        sb.AppendLine(Quote(venvPython) + " -m pip uninstall -y torch torchvision torchaudio");
        if (torchPlan.UseCuda)
        {
            sb.AppendLine("echo Installing CUDA PyTorch (" + torchPlan.CudaTag + ")...");
            sb.AppendLine(Quote(venvPython) + " -m pip install torch torchvision --index-url " + torchPlan.IndexUrl);
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  echo CUDA torch install failed. Falling back to CPU torch.");
            sb.AppendLine("  " + Quote(venvPython) + " -m pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu");
            sb.AppendLine("  if errorlevel 1 goto :fail");
            sb.AppendLine(")");
        }
        else
        {
            sb.AppendLine("echo Installing CPU PyTorch...");
            sb.AppendLine(Quote(venvPython) + " -m pip install torch torchvision --index-url " + torchPlan.IndexUrl);
            sb.AppendLine("if errorlevel 1 goto :fail");
        }
        sb.AppendLine("echo Installing requirements...");
        sb.AppendLine(Quote(venvPython) + " -m pip install -r " + Quote(requirements));
        sb.AppendLine("if errorlevel 1 goto :fail");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Checking torch.cuda...");
        sb.AppendLine(Quote(venvPython) + " -c \"import torch; print('torch', torch.__version__); print('cuda', torch.cuda.is_available()); print('device', torch.cuda.get_device_name(0) if torch.cuda.is_available() else 'cpu')\"");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Setup finished.");
        sb.AppendLine("pause");
        sb.AppendLine("exit /b 0");
        sb.AppendLine(":fail");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Setup failed.");
        sb.AppendLine("pause");
        sb.AppendLine("exit /b 1");
        return sb.ToString();
    }

    static string BuildYoloTrainBat(
        string serverDir,
        string venvPython,
        string scriptPath,
        string dataYaml,
        int epochs,
        int batch,
        string datasetName,
        string resultPath,
        string weightsDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal");
        sb.AppendLine("set PYTHONUNBUFFERED=1");
        sb.AppendLine("cd /d " + Quote(serverDir));
        sb.AppendLine("title YOLO train");
        sb.AppendLine("echo Python " + RequiredVersionLabel + " venv");
        sb.AppendLine("echo " + venvPython);
        sb.AppendLine("echo Dataset: " + datasetName);
        sb.AppendLine("echo.");
        sb.AppendLine(
            Quote(venvPython) +
            " -u " + Quote(scriptPath) +
            " --data " + Quote(dataYaml) +
            " --epochs " + epochs.ToString(CultureInfo.InvariantCulture) +
            " --batch " + batch.ToString(CultureInfo.InvariantCulture) +
            " --workers 0" +
            " --dataset " + Quote(datasetName) +
            " --result " + Quote(resultPath) +
            " --weights-dir " + Quote(weightsDir));
        sb.AppendLine("if errorlevel 1 goto :fail");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Training finished.");
        sb.AppendLine("exit /b 0");
        sb.AppendLine(":fail");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Training failed.");
        sb.AppendLine("pause");
        sb.AppendLine("exit /b 1");
        return sb.ToString();
    }

    static string BuildYoloServerBat(string serverDir, string venvPython, string scriptPath, string weightsPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal");
        sb.AppendLine("set PYTHONUNBUFFERED=1");
        sb.AppendLine("cd /d " + Quote(serverDir));
        sb.AppendLine("title YOLO server");
        sb.AppendLine("echo Python " + RequiredVersionLabel + " venv");
        sb.AppendLine("echo Weights: " + SanitizeCmdText(weightsPath));
        sb.AppendLine("echo Health: http://" + ServerHost + ":" + ServerPort.ToString(CultureInfo.InvariantCulture) + "/health");
        sb.AppendLine("echo.");
        sb.AppendLine(
            Quote(venvPython) +
            " -u " + Quote(scriptPath) +
            " --weights " + Quote(weightsPath) +
            " --host " + ServerHost +
            " --port " + ServerPort.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("if errorlevel 1 pause");
        sb.AppendLine("exit /b %ERRORLEVEL%");
        return sb.ToString();
    }

    static void WriteTextUtf8(string path, string contents)
    {
        File.WriteAllText(path, contents ?? "", new UTF8Encoding(false));
    }

    static string Quote(string value)
    {
        if (value == null)
            value = "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    static string SanitizeCmdText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace("&", " ").Replace("|", " ").Replace(">", " ").Replace("<", " ").Replace("%", " ");
    }
}