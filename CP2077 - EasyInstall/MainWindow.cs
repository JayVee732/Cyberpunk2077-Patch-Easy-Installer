using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace CP2077___EasyInstall
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private string generalPath = string.Empty;
        private static readonly Version CurrentProgramVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static int keyPress = 192;

        private static readonly string CurrentDir = Directory.GetCurrentDirectory();
        private static readonly string GamePathFilePath = Path.Combine(CurrentDir, "game_path");

        /// <summary>
        /// Entry point for the application
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            CheckForUpdate();
            try
            {
                // Check if the patch is already installed. If game_path file != NULL == already installed.
                string myPath = File.ReadAllText(GamePathFilePath);
                TraceDebugWrite(myPath);

                generalPath = myPath;
                btnMain.Text = "Patch already installed!";
                btnMain.Enabled = false;
                btnFindSteam.Enabled = false;
                btnFindGoG.Enabled = false;

                LoadSettings(generalPath);
                TraceDebugWrite("Patch already installed!");
            }
            catch (Exception)
            {
                TraceDebugWrite("Patch not already installed!");
            }
        }

        private void CheckForUpdate()
        {
            Version latestVersion;

            try
            {
                latestVersion = UpdateUtil.GetLatestVersion();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception while checking for latest version: {ex}");
                return;
            }
            if (latestVersion > CurrentProgramVersion)
            {
                DialogResult result = MetroFramework.MetroMessageBox.Show(this, "\nA new version is available, would you like to download it?", "New Version!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Process.Start("https://github.com/LittleZen/Cyberpunk2077-Patch-Easy-Installer/releases/latest");
                    Environment.Exit(1);
                }
                else
                {
                    lblUpdate.Text = "Update available, click here!";
                }
            }
        }

        /// <summary>
        /// Load the settings from config.json to autocheck options.
        /// </summary>
        /// <param name="generalPath">Location to main game installation.</param>
        private void LoadSettings(string generalPath)
        {
            var data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(Path.Combine(generalPath, "plugins", "cyber_engine_tweaks", "config.json")));

            cbAVX.Checked = data.AVX;
            numCpuMem.Value = data.CPUMemoryPoolFraction;
            cbAntialiasing.Checked = data.DisableAntialiasing;
            cbAsyncCompute.Checked = data.DisableAsyncCompute;
            numGpuMem.Value = data.GPUMemoryPoolFraction;
            cbMemoryPool.Checked = data.MemoryPool;
            cbRemovePedestrians.Checked = data.RemovePedestrians;
            cbSkipStartMenu.Checked = data.SkipStartMenu;
            cbSMT.Checked = data.SMT;
            cbSpectre.Checked = data.Spectre;
            cbDebug.Checked = data.UnlockMenu;
            cbVInput.Checked = data.VirtualInput;
            cbVInput.Checked = data.VirtualInput;
            cbConsole.Checked = data.Console;
            cbDumpOption.Checked = data.DumpOption;
            cbBoundaryTeleport.Checked = data.DisableBoundaryTeleport;
            cbIntroMovies.Checked = data.DisableIntroMovies;
            cbVignette.Checked = data.DisableVignette;
            txtConsoleKey.Text = new KeysConverter().ConvertToString(data.ConsoleKey);
            numConsoleKey.Value = data.ConsoleKey;
        }

        /// <summary>
        /// Initialize the copy function.
        /// </summary>
        /// <param name="sourceDirectory">Source of the files to be copied.</param>
        /// <param name="targetDirectory">Destination of the files to be copied to.</param>
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        /// <summary>
        /// Copy the files from the Patch folder and move them to the
        /// game installation folder.
        /// </summary>
        /// <param name="source">Source of the files to be copied.</param>
        /// <param name="target">Destination of the files to be copied to.</param>
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                TraceDebugWrite($@"Copying {target.FullName}\{fi.Name}");
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        /// <summary>
        /// Button Main is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnMain_Click(object sender, EventArgs e)
        {
            btnMain.Text = "Working...";
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string gamePath = Path.Combine(fbd.SelectedPath, "bin", "x64");
                    if (string.IsNullOrWhiteSpace(generalPath))
                    {
                        generalPath = gamePath;
                    }
                    PatchGame(gamePath);
                }
                else if (result == DialogResult.Cancel)
                {
                    MetroFramework.MetroMessageBox.Show(this, null, "Install Cancelled.", MessageBoxButtons.OK);
                    btnMain.Text = "Select Path to Cyberpunk 2077 Main Directory";
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "The tool wasn't able to open the dialog box!", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnMain.Text = "Critical Error!";
                }
            }
        }

        /// <summary>
        /// Copy the patch files to the game directory.
        /// </summary>
        /// <param name="gamePath">Location of the game files.</param>
        private void PatchGame(string gamePath)
        {
            TraceDebugWrite($"Path Selected: {gamePath}", "Message");

            try
            {
                DownloadLatestVersion();
                // Move files from patch to Cyberpunk 2077 path.
                btnMain.Text = "Installing...";
                string targetDirectory = gamePath;
                Copy("Patch", targetDirectory);

                using (StreamWriter outputFile = new StreamWriter(GamePathFilePath))
                    outputFile.Write(gamePath);

                TraceDebugWrite($"game_path path = {GamePathFilePath}");

                // Write Path file. It is used for checking if the patch has already been installed (on next restart)
                File.WriteAllText(GamePathFilePath, gamePath);
                LoadSettings(gamePath);

                TraceDebugWrite("Path correctly created!\n");

                // Remove the Release.zip and Release folder after extraction.
                string removeReleaseZip = Path.Combine(CurrentDir, "Release.zip");
                string downloadPath = Path.Combine(CurrentDir, "Patch");

                File.Delete(removeReleaseZip);
                Directory.Delete(downloadPath, true);

                MetroFramework.MetroMessageBox.Show(this, "Patch successfully installed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Question);
                btnMain.Text = "Successfully Installed!";
                btnMain.Enabled = false;
                btnFindSteam.Enabled = false;
                btnFindGoG.Enabled = false;
            }
            catch (IOException ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error during installation\n\n{ex.InnerException}", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error during installation\n\n{ex.InnerException}", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnMain.Text = "Critical Error!";
            }
        }

        /// <summary>
        /// Button About has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAbout_Click(object sender, EventArgs e)
        {
            Process.Start("https://bit.ly/385MEvl");
        }

        /// <summary>
        /// Button Save has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string settingsPath = Path.Combine(generalPath, "plugins", "cyber_engine_tweaks", "config.json");

                var data = new Data()
                {
                    AVX = cbAVX.Checked,
                    SMT = cbSMT.Checked,
                    Spectre = cbMemoryPool.Checked,
                    VirtualInput = cbSpectre.Checked,
                    MemoryPool = cbMemoryPool.Checked,
                    UnlockMenu = cbDebug.Checked,
                    CPUMemoryPoolFraction = numCpuMem.Value,
                    GPUMemoryPoolFraction = numGpuMem.Value,
                    RemovePedestrians = cbRemovePedestrians.Checked,
                    SkipStartMenu = cbSkipStartMenu.Checked,
                    DisableAsyncCompute = cbAsyncCompute.Checked,
                    DisableAntialiasing = cbAntialiasing.Checked,
                    Console = cbConsole.Checked,
                    DumpOption = cbDumpOption.Checked,
                    DisableBoundaryTeleport = cbBoundaryTeleport.Checked,
                    DisableIntroMovies = cbIntroMovies.Checked,
                    DisableVignette = cbVignette.Checked,
                    ConsoleKey = keyPress
                };

                using (StreamWriter file = File.CreateText(settingsPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    // Serialize object directly into file stream.
                    serializer.Serialize(file, data);
                }
                MetroFramework.MetroMessageBox.Show(this, "\nSaved!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
            catch (Exception)
            {
                MetroFramework.MetroMessageBox.Show(this, "\nYou must install the patch before save the settings!", "Patch not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Button Settings has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSettings_Click(object sender, EventArgs e)
        {
            try
            {
                string settingsPath = Path.Combine(generalPath, "plugins", "cyber_engine_tweaks", "config.json");
                Process.Start(settingsPath);
            }
            catch (Exception)
            {
                MetroFramework.MetroMessageBox.Show(this, "You must select a valid path before open the settings!", "Exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Pull the latest version of the yamashi fixes.
        /// </summary>
        private void DownloadLatestVersion()
        {
            try
            {
                string downloadPath = Path.Combine(CurrentDir, "Patch");
                string zipDownloadFile = Path.Combine(CurrentDir, "Release.zip");

                using (FileStream zipFile = File.Create(zipDownloadFile))

                    btnMain.Text = "Downloading...";

                using (var httpclient = new WebClient())
                {
                    httpclient.DownloadFile("https://github.com/yamashi/PerformanceOverhaulCyberpunk/releases/latest/download/Release.zip", zipDownloadFile);
                }

                if (Directory.Exists(downloadPath))
                    Directory.Delete(downloadPath, true);

                btnMain.Text = "Extracting...";
                ZipFile.ExtractToDirectory(zipDownloadFile, downloadPath);
            }
            catch (IOException ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error during installation\n{ex.InnerException}", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnMain.Text = "Critical Error!";
            }
            catch (WebException ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error during installation\n{ex.InnerException}", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnMain.Text = "Critical Error!";
            }
        }

        /// <summary>
        /// Button Update has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                PatchGame(File.ReadAllText(GamePathFilePath));
                btnMain.Text = "Successfully Installed!";
            }
            catch (Exception)
            {
                TraceDebugWrite("Main path not selected, can not check for updates!");
                MetroFramework.MetroMessageBox.Show(this, "Please select the Cyberpunk 2077 main folder before checking for updates!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Button Open Patch Logs has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLogs_Click(object sender, EventArgs e)
        {
            string logPath = Path.Combine(generalPath, "plugins", "cyber_engine_tweaks", "cyber_engine_tweaks.log");
            try
            {
                Process.Start(logPath);
            }
            catch (Exception)
            {
                MetroFramework.MetroMessageBox.Show(this, "File not found, you need to run the game at least one time before a log file can be generated!", "Exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Button Uninstall has been clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                btnMain.Text = "Uninstalling...";
                // Delete plugins directory recursively
                Directory.Delete(Path.Combine(generalPath, "plugins"), true);

                // Delete version.dll file
                File.Delete(Path.Combine(generalPath, "version.dll"));

                // Delete game_path file
                File.Delete(GamePathFilePath);

                // Unlock main_button for reinstall the patch
                btnMain.Text = "Select Path To Cyberpunk 2077 Main Directory";
                btnMain.Enabled = true;
                btnFindSteam.Enabled = true;
                btnFindGoG.Enabled = true;

                MetroFramework.MetroMessageBox.Show(this, "Successfully uninstalled", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
            catch (Exception)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Unable to delete mod files.\nPlease remove {generalPath}\\version.dll and {generalPath}\\plugins\\ manually.", "Exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnMain.Text = "Manually Select Path to Cyberpunk 2077 Main Directory";
            }
        }
        private void btnFindSteam_Click(object sender, EventArgs e)
        {
            try
            {
                btnMain.Text = "Working...";
                string path = SteamGamePath.FindGameByAppID("1091500");
                if (path == null)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Error: Couldn't Find Cyberpunk for Steam!", "File not found Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //TraceDebugWrite("Error: Couldn't Find CyberPunk for Steam!");
                    btnMain.Text = "Select Path to Cyberpunk 2077 Main Directory";
                    return;
                }

                DialogResult result = MetroFramework.MetroMessageBox.Show(this, path, "Is this Correct?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (string.IsNullOrWhiteSpace(generalPath))
                    {
                        generalPath = Path.Combine(path, "bin", "x64");
                    }
                    PatchGame(Path.Combine(path, "bin", "x64"));
                }
                else if (result == DialogResult.No)
                {
                    MetroFramework.MetroMessageBox.Show(this, null, "Install Cancelled.", MessageBoxButtons.OK);
                    btnMain.Text = "Select Path to Cyberpunk 2077 Main Directory";
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "The tool wasn't able to open the dialog box!", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnMain.Text = "Critical Error!";
                }
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error: {ex}", "Unknown Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //TraceDebugWrite("Error" + ex);
            }
        }

        private void btnFindGoG_Click(object sender, EventArgs e)
        {
            try
            {
                btnMain.Text = "Working...";
                string path = GoGGamePath.FindGameByAppID("1423049311");
                if (path == null)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Error: Couldn't Find Cyberpunk for GOG!", "Error: File not found!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //TraceDebugWrite("Error: Couldn't Find CyberPunk for GoG!");
                    btnMain.Text = "Select Path to Cyberpunk 2077 Main Directory";
                    return;
                }

                DialogResult result = MetroFramework.MetroMessageBox.Show(this, path, "Is this Correct?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (string.IsNullOrWhiteSpace(generalPath))
                    {
                        generalPath = Path.Combine(path, "bin", "x64");
                    }
                    PatchGame(Path.Combine(path, "bin", "x64"));
                }
                else if (result == DialogResult.No)
                {
                    MetroFramework.MetroMessageBox.Show(this, null, "Install Cancelled.", MessageBoxButtons.OK);
                    btnMain.Text = "Select Path to Cyberpunk 2077 Main Directory";
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "The tool wasn't able to open the dialog box!", "Critical Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, $"Error: {ex}", "Unknown Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //TraceDebugWrite("Error" + ex);
            }
        }

        private void lblUpdate_Click(object sender, EventArgs e)
        {
            if (lblUpdate.Text == "Update available, click here!")
            {
                Process.Start("https://github.com/LittleZen/Cyberpunk2077-Patch-Easy-Installer/releases/latest");
                Environment.Exit(1);
            }
            else
            {
                MetroFramework.MetroMessageBox.Show(this, "\nYou are running the latest version!", "Update", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
        }

        private void txtConsoleKey_KeyUp(object sender, KeyEventArgs e)
        {
            keyPress = e.KeyValue;
            txtConsoleKey.Text = e.KeyCode.ToString();
            numConsoleKey.Value = e.KeyValue;
            TraceDebugWrite($"Keyboard key pressed: {e.KeyCode} - Value: {keyPress}");
        }

        private void numConsoleKey_ValueChanged(object sender, EventArgs e)
        {
            keyPress = Convert.ToInt32(numConsoleKey.Value);
            txtConsoleKey.Text = new KeysConverter().ConvertToString(keyPress);
        }

        // TODO: Move to separate Logger class?
        private static void TraceDebugWrite(string message, string category = null)
        {
#if DEBUG
            Trace.WriteLine(message, category);
#endif
        }

        private void cbConsole_CheckedChanged(object sender, EventArgs e)
        {
            if (cbConsole.Checked == false)
            {
                txtConsoleKey.Enabled = false;
                numConsoleKey.Enabled = false;
            }
            else
            {
                txtConsoleKey.Enabled = true;
                numConsoleKey.Enabled = true;
            }
        }
    }
}
