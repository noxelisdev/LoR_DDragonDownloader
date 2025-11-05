using LoR_DataDragonDownloader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace LoR_DDragonDownloader
{
    public partial class MainForm : Form
    {
        public string appVersion = "1.3.2";
        static public string appLanguage = "en";

        public string baseUrl = "https://dd.b.pvp.net/";
        public List<string> versions = new List<string>();
        public List<string> langs = new List<string>();
        public List<string> sets = new List<string>();

        public bool downloadingProcessRunning = false;

        public MainForm()
        {
            InitializeComponent();
            DetermineAppLanguage();
            GetVersionsList();
            GetLangsList();
            GetSetsList();
            InitFormTexts();
        }

        /**
         * Define the current language of the app, based on app's settings.
         */
        public void DetermineAppLanguage()
        {
            // Determine prefered language from settings file
            appLanguage = AppSettings.GetCurrentSettings()["language"].ToString();

            // Updating UI language selector with language from settings
            switch (appLanguage)
            {
                case "fr":
                    MainForm_LanguageSelector.SelectedItem = "Français";
                    break;
                case "en":
                default:
                    MainForm_LanguageSelector.SelectedItem = "English";
                    break;
            }
        }

        /**
         * Downloading and preparing a list of all available versions of Legends of Runeterra's Data Dragon.
         */
        public void GetVersionsList()
        {
            string fileLink = "https://github.com/InFinity54/LoR_DDragonDownloader/raw/master/LoR_DDragonDownloader/database/runeterra_versions.json";
            string jsonString = new WebClient().DownloadString(fileLink);
            JArray json = JArray.Parse(jsonString);

            foreach (string version in json)
            {
                versions.Add(version);
            }
        }

        /**
         * Downloading and preparing a list of all available languages of Legends of Runeterra's Data Dragon.
         */
        public void GetLangsList()
        {
            string fileLink = "https://github.com/InFinity54/LoR_DDragonDownloader/raw/master/LoR_DDragonDownloader/database/runeterra_langs.json";
            string jsonString = new WebClient().DownloadString(fileLink);
            JArray json = JArray.Parse(jsonString);

            foreach (string version in json)
            {
                langs.Add(version);
            }
        }

        /**
         * Downloading and preparing a list of all available sets of Legends of Runeterra's Data Dragon.
         */
        public void GetSetsList()
        {
            string fileLink = "https://github.com/InFinity54/LoR_DDragonDownloader/raw/master/LoR_DDragonDownloader/database/runeterra_sets.json";
            string jsonString = new WebClient().DownloadString(fileLink);
            JArray json = JArray.Parse(jsonString);

            foreach (string version in json)
            {
                sets.Add(version);
            }
        }

        /**
         * Set translated texts on app window, instead of placeholders, before showing it.
         * Note : Progress texts are handled directly during downloading process, if running.
         */
        public void InitFormTexts()
        {
            App_Version.Text = "Version " + appVersion;

            MainForm_Group_Language.Text = TranslationSystem.LanguageAreaTitle();
            MainForm_Description.Text = TranslationSystem.DownloaderWelcomeMessage();
            MainForm_Group_DownloadMode.Text = TranslationSystem.DownloadModeAreaTitle();
            MainForm_DownloadModeLight.Text = TranslationSystem.DownloadModeLastVersionOnly();
            MainForm_DownloadModeFull.Text = TranslationSystem.DownloadModeAllVersions();
            MainForm_DownloadModeExtractOnly.Text = TranslationSystem.DownloadModeLastVersionExtractOnly();
            MainForm_DownloadModeGenerateLinksOnly.Text = TranslationSystem.DownloadModeLastVersionLinksOnly();
            MainForm_Group_SortType.Text = TranslationSystem.DownloadSortAreaTitle();
            MainForm_SortInOneFolder.Text = TranslationSystem.DownloadSortInOneFolder();
            MainForm_SortBySet.Text = TranslationSystem.DownloadSortBySet();
            MainForm_Group_DownloadProgress.Text = TranslationSystem.ProgressAreaTitle();

            if (!downloadingProcessRunning)
            {
                if (MainForm_GlobalProgressBar.Value == 0)
                {
                    MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.WaitingDDragonDownloadStart();
                    MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.WaitingDDragonDownloadStart();
                    MainForm_GlobalProgressLabel.Text = TranslationSystem.WaitingDDragonDownloadStart();
                }
                else
                {
                    MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.FinishedMessage();
                    MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.FinishedMessage();
                    MainForm_GlobalProgressLabel.Text = TranslationSystem.FinishedMessage();
                }
            }

            MainForm_Group_Settings.Text = TranslationSystem.DownloadSettingsAreaTitle();
            MainForm_Settings_DownloadFolder_Label.Text = TranslationSystem.DownloadFolderSelectionHint();
            MainForm_Settings_DownloadFolder_Browse.Text = TranslationSystem.DownloadFolderSelectionButton();
            MainForm_Button_StartDownload.Text = TranslationSystem.DownloadStartButton();
            MainForm_Button_Quit.Text = TranslationSystem.AppExitButton();
        }

        private void MainForm_Button_Quit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(TranslationSystem.ExitModalConfirmMessage(), MainForm_Title.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void MainForm_Button_StartDownload_Click(object sender, EventArgs e)
        {
            // Handling required form inputs
            if (!MainForm_DownloadModeLight.Checked == true && !MainForm_DownloadModeFull.Checked == true && !MainForm_DownloadModeGenerateLinksOnly.Checked == true && !MainForm_DownloadModeExtractOnly.Checked == true)
            {
                MessageBox.Show(TranslationSystem.DownloadStartUnknownDownloadModeError(), MainForm_Title.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!MainForm_DownloadModeGenerateLinksOnly.Checked == true && !MainForm_SortInOneFolder.Checked == true && !MainForm_SortBySet.Checked == true)
            {
                MessageBox.Show(TranslationSystem.DownloadStartUnknownSortModeError(), MainForm_Title.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MainForm_Settings_DownloadFolder_TextBox.Text.Length == 0)
            {
                MessageBox.Show(TranslationSystem.DownloadStartUnknownDownloadFolderError(), MainForm_Title.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Creating download folder if not exist
            if (!Directory.Exists(MainForm_Settings_DownloadFolder_TextBox.Text))
            {
                Directory.CreateDirectory(MainForm_Settings_DownloadFolder_TextBox.Text);
            }

            // Handling download folder subdirectories and foldey (if any)
            if (MainForm_DownloadModeExtractOnly.Checked != true && (Directory.GetDirectories(MainForm_Settings_DownloadFolder_TextBox.Text).Length > 0 || Directory.GetFiles(MainForm_Settings_DownloadFolder_TextBox.Text).Length > 0))
            {
                if (MessageBox.Show(TranslationSystem.DownloadFolderIsNotEmptyMessage(), MainForm_Title.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    Directory.Delete(MainForm_Settings_DownloadFolder_TextBox.Text, true);
                    Directory.CreateDirectory(MainForm_Settings_DownloadFolder_TextBox.Text);
                    StartingDownload();
                }
            }
            else
            {
                StartingDownload();
            }
        }

        private void StartingDownload()
        {
            // Stop handling translation for progress texts
            downloadingProcessRunning = true;

            // Editing progress bars style
            MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
            MainForm_CurrentVersionProgressBar.Style = ProgressBarStyle.Blocks;
            MainForm_GlobalProgressBar.Style = ProgressBarStyle.Blocks;

            // Blocking editing of download settings
            MainForm_Settings_DownloadFolder_TextBox.ReadOnly = true;
            MainForm_Settings_DownloadFolder_Browse.Enabled = false;
            MainForm_DownloadModeLight.Enabled = false;
            MainForm_DownloadModeFull.Enabled = false;
            MainForm_DownloadModeExtractOnly.Enabled = false;
            MainForm_DownloadModeGenerateLinksOnly.Enabled = false;
            MainForm_SortInOneFolder.Enabled = false;
            MainForm_SortBySet.Enabled = false;

            // Disabling "Start download" button
            MainForm_Button_StartDownload.Enabled = false;

            DataDragonWorker.RunWorkerAsync();
        }

        private void DataDragonWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (MainForm_DownloadModeLight.Checked)
            {
                // Starting download process for latest version only
                Invoke((MethodInvoker)delegate ()
                {
                    MainForm_GlobalProgressLabel.Text = TranslationSystem.DownloadOfLatestVersionIsRunning();
                });
                DownloadDDragonForLatestVersion(versions[versions.Count - 1]);
            }
            else if (MainForm_DownloadModeFull.Checked)
            {
                // Starting download process for all versions
                Invoke((MethodInvoker)delegate ()
                {
                    MainForm_GlobalProgressLabel.Text = TranslationSystem.DownloadOfAllVersionsIsRunning();
                });
                DownloadDDragonForAllVersions();
            }
            else if (MainForm_DownloadModeExtractOnly.Checked)
            {
                // Starting extracting pre-downloaded files process for latest version only
                ExtractDDragonFiles(versions[versions.Count - 1]);
            }
            else
            {
                // Starting download links list generation for latest version only
                GenerateDDragonDownloadLinksList(versions[versions.Count - 1]);
            }
        }

        private bool IsFileAvailable(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        private void DataDragonFileDownloading(object sender, DownloadProgressChangedEventArgs e, string fileName)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());

            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.DownloadingFileMessageWithProgress(fileName, Math.Round((e.BytesReceived / 1024f) / 1024f, 2), Math.Round((e.TotalBytesToReceive / 1024f) / 1024f, 2));
                MainForm_CurrentTaskProgressBar.Value = int.Parse(Math.Truncate(bytesIn / totalBytes * 100).ToString());
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
            });
        }

        private void DownloadDDragonForSpecificVersion(string version)
        {
            // Configure progress bars for download process
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentVersionProgressBar.Maximum = sets.Count * langs.Count;
                MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.DownloadingVersionMessage(version);
                MainForm_CurrentTaskProgressBar.Maximum = 100;
                MainForm_CurrentTaskProgressBar.Value = 0;
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
            });

            foreach (string set in sets)
            {
                foreach (string lang in langs)
                {
                    string fileName = set + "-" + lang + ".zip";
                    string fileURL = baseUrl + version.Replace(".", "_") + "/" + fileName;

                    // Checking if remote file exists
                    if (IsFileAvailable(fileURL))
                    {
                        string downloadPath = MainForm_Settings_DownloadFolder_TextBox.Text + "\\" + version;

                        // Creating download path, if not exists
                        if (!Directory.Exists(downloadPath))
                        {
                            Directory.CreateDirectory(downloadPath);
                        }

                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.DownloadingFileMessage(fileName);
                        });

                        // Handling file download and download progress
                        bool downloadFinished = false;
                        WebClient client = new WebClient();
                        client.DownloadProgressChanged += (sender, e) => DataDragonFileDownloading(sender, e, fileName);
                        client.DownloadFileCompleted += (s, e) =>
                        {
                            downloadFinished = true;
                        };
                        client.DownloadFileAsync(new Uri(fileURL), downloadPath + "\\" + fileName);
                        while (!downloadFinished) { }

                        // Handling file extraction
                        string fileToExtract = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version, fileName);
                        string extractDirectory = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version, set);
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.ExtractingFileMessage(fileName);
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
                        });

                        using (FileStream zipToOpen = new FileStream(fileToExtract, FileMode.Open, FileAccess.Read))
                        {
                            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                            {
                                int totalFilesToExtract = archive.Entries.Count;
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Maximum = totalFilesToExtract;
                                });

                                int extractedFilesCount = 0;

                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    string destinationPath = Path.Combine(extractDirectory, entry.FullName);
                                    string directoryPath = Path.GetDirectoryName(destinationPath);

                                    if (!string.IsNullOrEmpty(directoryPath))
                                    {
                                        Directory.CreateDirectory(directoryPath);
                                    }

                                    entry.ExtractToFile(destinationPath, overwrite: true);
                                    extractedFilesCount++;

                                    Invoke((MethodInvoker)delegate ()
                                    {
                                        MainForm_CurrentTaskProgressBar.Value = extractedFilesCount;
                                    });
                                }
                            }
                        }

                        // Handling folder refactoring
                        string versionRootFolder = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version, set);
                        bool specialExtract = false;

                        // Fix for version 1.3.0
                        if (Directory.Exists(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName))))
                        {
                            specialExtract = true;
                            List<String> tmpFilesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName)), "*.*", SearchOption.AllDirectories).ToList();
                            int totalTmpFilesToMove = tmpFilesToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.PreparingFolderMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalTmpFilesToMove;
                            });

                            foreach (string tmpFile in tmpFilesToMove)
                            {
                                string newTmpFile = tmpFile.Replace("\\" + Path.GetFileNameWithoutExtension(fileName), "");

                                if (!Directory.Exists(Path.GetDirectoryName(newTmpFile)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newTmpFile));
                                }

                                File.Move(tmpFile, newTmpFile, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            Directory.Delete(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName)), true);
                        }

                        // If current set is not "core" or "adventure", cards images are handled first
                        if (set != "core" && set != "adventure")
                        {
                            List<String> cardsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "cards"), "*.*").ToList();
                            int totalCardsToMove = cardsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingCardsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalCardsToMove;
                            });

                            foreach (string card in cardsToMove)
                            {
                                string newCard = Path.Combine(Path.GetDirectoryName(card), lang, Path.GetFileName(card));

                                if (!Directory.Exists(Path.GetDirectoryName(newCard)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newCard));
                                }

                                File.Move(card, newCard, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }
                        }

                        // If current set is "adventure", handle images for powers, relics & items of Path of Champions
                        if (set == "adventure")
                        {
                            // Powers images
                            List<String> powersToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "powers"), "*.*").ToList();
                            int totalPowersToMove = powersToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingPowersPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalPowersToMove;
                            });

                            foreach (string power in powersToMove)
                            {
                                string newPower = Path.Combine(Path.GetDirectoryName(power), lang, Path.GetFileName(power));

                                if (!Directory.Exists(Path.GetDirectoryName(newPower)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newPower));
                                }

                                File.Move(power, newPower, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            // Relics images
                            List<String> relicsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "relics"), "*.*").ToList();
                            int totalRelicsToMove = relicsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingRelicsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalRelicsToMove;
                            });

                            foreach (string relic in relicsToMove)
                            {
                                string newRelic = Path.Combine(Path.GetDirectoryName(relic), lang, Path.GetFileName(relic));

                                if (!Directory.Exists(Path.GetDirectoryName(newRelic)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newRelic));
                                }

                                File.Move(relic, newRelic, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            // Items images
                            List<String> itemsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "items"), "*.*").ToList();
                            int totalItemsToMove = itemsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingItemsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalItemsToMove;
                            });

                            foreach (string item in itemsToMove)
                            {
                                string newItem = Path.Combine(Path.GetDirectoryName(item), lang, Path.GetFileName(item));

                                if (!Directory.Exists(Path.GetDirectoryName(newItem)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newItem));
                                }

                                File.Move(item, newItem, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }
                        }

                        // Recreating "data" folder if files extracted in a subfolder
                        if (specialExtract && set != "core")
                        {
                            string originalFile = Path.Combine(extractDirectory, lang, "data.json");
                            string destinationFile = Path.Combine(extractDirectory, lang, "data", set + "-" + lang + ".json");

                            if (!Directory.Exists(Path.Combine(extractDirectory, lang, "data")))
                            {
                                Directory.CreateDirectory(Path.Combine(extractDirectory, lang, "data"));
                            }

                            File.Move(originalFile, destinationFile);
                        }

                        List<string> filesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, lang, "data"), "*.*", SearchOption.AllDirectories).ToList();
                        int totalFilesToMove = filesToMove.Count;
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingFilesMessage();
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = totalFilesToMove;
                        });

                        foreach (string file in filesToMove)
                        {
                            string newFile = "";

                            if (MainForm_SortInOneFolder.Checked == true)
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "data"), Path.Combine(versionRootFolder, "..", "data"));
                            }
                            else
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "data"), Path.Combine(versionRootFolder, "data"));
                            }

                            if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                            }

                            File.Move(file, newFile, true);
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressBar.Value++;
                            });
                        }

                        filesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, lang, "img"), "*.*", SearchOption.AllDirectories).ToList();
                        totalFilesToMove = filesToMove.Count;
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = totalFilesToMove;
                        });

                        foreach (string file in filesToMove)
                        {
                            string newFile = "";

                            if (MainForm_SortInOneFolder.Checked == true)
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "img"), Path.Combine(versionRootFolder, "..", "img"));
                            }
                            else
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "img"), Path.Combine(versionRootFolder, "img"));
                            }

                            if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                            }

                            File.Move(file, newFile, true);
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressBar.Value++;
                            });
                        }

                        Directory.Delete(Path.Combine(versionRootFolder, lang), true);

                        // Une fois le dossier réorganisé, on supprime les fichiers inutiles
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.CleaningMessage();
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = 100;
                            MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Marquee;
                        });
                        File.Delete(Path.Combine(extractDirectory, "COPYRIGHT"));
                        File.Delete(Path.Combine(extractDirectory, "metadata.json"));
                        File.Delete(Path.Combine(extractDirectory, "README.md"));
                        File.Delete(Path.Combine(downloadPath, fileName));

                        if (MainForm_SortInOneFolder.Checked == true)
                        {
                            Directory.Delete(extractDirectory, true);
                        }
                    }

                    Invoke((MethodInvoker)delegate ()
                    {
                        MainForm_CurrentVersionProgressBar.Value++;
                    });
                }
            }

            // Generating metadata.json
            string metadataSavePath = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version);

            if (MainForm_SortInOneFolder.Checked == true || (MainForm_SortBySet.Checked == true && Directory.Exists(metadataSavePath)))
            {
                string cardsFolder = "";
                Metadata metadata = new Metadata();
                Invoke((MethodInvoker)delegate ()
                {
                    MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MetadataJsonFileGeneratingMessage();
                    MainForm_CurrentTaskProgressBar.Value = 0;
                    MainForm_CurrentTaskProgressBar.Maximum = 100;
                    MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Marquee;
                });

                if (MainForm_SortInOneFolder.Checked == true)
                {
                    cardsFolder = Path.Combine(metadataSavePath, "img", "cards");
                }
                else
                {
                    cardsFolder = Path.Combine(metadataSavePath, "set1", "img", "cards");
                }


                foreach (string folder in Directory.GetDirectories(cardsFolder))
                {
                    metadata.locales.Add(folder.Replace(cardsFolder, "").Replace("\\", ""));
                }

                File.WriteAllText(Path.Combine(metadataSavePath, "metadata.json"), JsonConvert.SerializeObject(metadata, Formatting.Indented));
            }
        }

        private void DownloadDDragonForLatestVersion(string version)
        {
            // Configure progress bars for latest version only download process
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_GlobalProgressBar.Maximum = 1;
            });

            // Executing download process
            DownloadDDragonForSpecificVersion(version);

            // Updating progress bars, progress bar styles and progress texts
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentTaskProgressBar.Maximum = 100;
                MainForm_CurrentTaskProgressBar.Value = 100;
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentVersionProgressBar.Maximum = 100;
                MainForm_CurrentVersionProgressBar.Value = 100;
                MainForm_CurrentVersionProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_GlobalProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_GlobalProgressBar.Value = 1;
                MainForm_GlobalProgressBar.Style = ProgressBarStyle.Blocks;
            });

            // Reactivating UI
            DownloadFinished();
        }

        private void DownloadDDragonForAllVersions()
        {
            // Configure progress bars for latest version only download process
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_GlobalProgressBar.Maximum = versions.Count;
            });

            // Executing download process
            foreach (string version in versions)
            {
                DownloadDDragonForSpecificVersion(version);
                MainForm_GlobalProgressBar.Value++;
            }

            // Updating progress bars, progress bar styles and progress texts
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentTaskProgressBar.Maximum = 100;
                MainForm_CurrentTaskProgressBar.Value = 100;
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentVersionProgressBar.Maximum = 100;
                MainForm_CurrentVersionProgressBar.Value = 100;
                MainForm_CurrentVersionProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_GlobalProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_GlobalProgressBar.Value = MainForm_GlobalProgressBar.Maximum;
                MainForm_GlobalProgressBar.Style = ProgressBarStyle.Blocks;
            });

            // Reactivating UI
            DownloadFinished();
        }

        private void GenerateDDragonDownloadLinksList(string version)
        {
            string downloadLinksFileName = MainForm_Settings_DownloadFolder_TextBox.Text + "\\version_" + version + "_files_links.txt";
            List<string> downloadLinks = new List<string>();

            if (File.Exists(downloadLinksFileName))
            {
                File.Delete(downloadLinksFileName);
            }

            foreach (string set in sets)
            {
                foreach (string lang in langs)
                {
                    string fileName = set + "-" + lang + ".zip";
                    string fileURL = baseUrl + version.Replace(".", "_") + "/" + fileName;
                    downloadLinks.Add(fileURL);
                }
            }

            File.WriteAllLines(downloadLinksFileName, downloadLinks);

            if (MessageBox.Show(TranslationSystem.FinishedLinksGenerationAlertMessage(downloadLinksFileName), null, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(downloadLinksFileName)
                {
                    UseShellExecute = true
                };
                p.Start();
            }

            // Reactivating UI
            DownloadFinished();
        }

        private void ExtractDDragonFiles(string version)
        {
            // Configure progress bars for latest version only download process
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_GlobalProgressBar.Maximum = 1;
            });

            // Configure progress bars for download process
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentVersionProgressBar.Maximum = sets.Count * langs.Count;
                MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.DownloadingVersionMessage(version);
                MainForm_CurrentTaskProgressBar.Maximum = 100;
                MainForm_CurrentTaskProgressBar.Value = 0;
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
            });

            // Executing extract process
            foreach (string set in sets)
            {
                foreach (string lang in langs)
                {
                    string fileName = set + "-" + lang + ".zip";

                    if (File.Exists(MainForm_Settings_DownloadFolder_TextBox.Text + "\\" + fileName))
                    {
                        // Handling file extraction
                        string fileToExtract = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, fileName);
                        string extractDirectory = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version, set);
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.ExtractingFileMessage(fileName);
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
                        });

                        using (FileStream zipToOpen = new FileStream(fileToExtract, FileMode.Open, FileAccess.Read))
                        {
                            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                            {
                                int totalFilesToExtract = archive.Entries.Count;

                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Maximum = totalFilesToExtract;
                                    MainForm_CurrentTaskProgressBar.Value = 0;
                                });

                                int extractedFilesCount = 0;

                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    string destinationPath = Path.Combine(extractDirectory, entry.FullName);

                                    string directoryPath = Path.GetDirectoryName(destinationPath);
                                    if (!string.IsNullOrEmpty(directoryPath))
                                    {
                                        Directory.CreateDirectory(directoryPath);
                                    }

                                    if (!string.IsNullOrEmpty(entry.Name))
                                    {
                                        entry.ExtractToFile(destinationPath, overwrite: true);
                                    }

                                    extractedFilesCount++;

                                    Invoke((MethodInvoker)delegate ()
                                    {
                                        MainForm_CurrentTaskProgressBar.Value = extractedFilesCount;
                                    });
                                }
                            }
                        }

                        // Handling folder refactoring
                        string versionRootFolder = Path.Combine(MainForm_Settings_DownloadFolder_TextBox.Text, version, set);
                        bool specialExtract = false;

                        // Fix for version 1.3.0
                        if (Directory.Exists(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName))))
                        {
                            specialExtract = true;
                            List<String> tmpFilesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName)), "*.*", SearchOption.AllDirectories).ToList();
                            int totalTmpFilesToMove = tmpFilesToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.PreparingFolderMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalTmpFilesToMove;
                            });

                            foreach (string tmpFile in tmpFilesToMove)
                            {
                                string newTmpFile = tmpFile.Replace("\\" + Path.GetFileNameWithoutExtension(fileName), "");

                                if (!Directory.Exists(Path.GetDirectoryName(newTmpFile)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newTmpFile));
                                }

                                File.Move(tmpFile, newTmpFile, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            Directory.Delete(Path.Combine(versionRootFolder, Path.GetFileNameWithoutExtension(fileName)), true);
                        }

                        // If current set is not "core" or "adventure", cards images are handled first
                        if (set != "core" && set != "adventure")
                        {
                            List<String> cardsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "cards"), "*.*").ToList();
                            int totalCardsToMove = cardsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingCardsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalCardsToMove;
                            });

                            foreach (string card in cardsToMove)
                            {
                                string newCard = Path.Combine(Path.GetDirectoryName(card), lang, Path.GetFileName(card));

                                if (!Directory.Exists(Path.GetDirectoryName(newCard)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newCard));
                                }

                                File.Move(card, newCard, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }
                        }

                        // If current set is "adventure", handle images for powers, relics & items of Path of Champions
                        if (set == "adventure")
                        {
                            // Powers images
                            List<String> powersToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "powers"), "*.*").ToList();
                            int totalPowersToMove = powersToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingPowersPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalPowersToMove;
                            });

                            foreach (string power in powersToMove)
                            {
                                string newPower = Path.Combine(Path.GetDirectoryName(power), lang, Path.GetFileName(power));

                                if (!Directory.Exists(Path.GetDirectoryName(newPower)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newPower));
                                }

                                File.Move(power, newPower, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            // Relics images
                            List<String> relicsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "relics"), "*.*").ToList();
                            int totalRelicsToMove = relicsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingRelicsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalRelicsToMove;
                            });

                            foreach (string relic in relicsToMove)
                            {
                                string newRelic = Path.Combine(Path.GetDirectoryName(relic), lang, Path.GetFileName(relic));

                                if (!Directory.Exists(Path.GetDirectoryName(newRelic)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newRelic));
                                }

                                File.Move(relic, newRelic, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }

                            // Items images
                            List<String> itemsToMove = Directory.GetFiles(Path.Combine(extractDirectory, lang, "img", "items"), "*.*").ToList();
                            int totalItemsToMove = itemsToMove.Count;
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingItemsPicturesMessage();
                                MainForm_CurrentTaskProgressBar.Value = 0;
                                MainForm_CurrentTaskProgressBar.Maximum = totalItemsToMove;
                            });

                            foreach (string item in itemsToMove)
                            {
                                string newItem = Path.Combine(Path.GetDirectoryName(item), lang, Path.GetFileName(item));

                                if (!Directory.Exists(Path.GetDirectoryName(newItem)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newItem));
                                }

                                File.Move(item, newItem, true);
                                Invoke((MethodInvoker)delegate ()
                                {
                                    MainForm_CurrentTaskProgressBar.Value++;
                                });
                            }
                        }

                        // Recreating "data" folder if files extracted in a subfolder
                        if (specialExtract && set != "core")
                        {
                            string originalFile = Path.Combine(extractDirectory, lang, "data.json");
                            string destinationFile = Path.Combine(extractDirectory, lang, "data", set + "-" + lang + ".json");

                            if (!Directory.Exists(Path.Combine(extractDirectory, lang, "data")))
                            {
                                Directory.CreateDirectory(Path.Combine(extractDirectory, lang, "data"));
                            }

                            File.Move(originalFile, destinationFile);
                        }

                        List<string> filesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, lang, "data"), "*.*", SearchOption.AllDirectories).ToList();
                        int totalFilesToMove = filesToMove.Count;
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.MovingFilesMessage();
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = totalFilesToMove;
                        });

                        foreach (string file in filesToMove)
                        {
                            string newFile = "";

                            if (MainForm_SortInOneFolder.Checked == true)
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "data"), Path.Combine(versionRootFolder, "..", "data"));
                            }
                            else
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "data"), Path.Combine(versionRootFolder, "data"));
                            }

                            if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                            }

                            File.Move(file, newFile, true);
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressBar.Value++;
                            });
                        }

                        filesToMove = Directory.GetFiles(Path.Combine(versionRootFolder, lang, "img"), "*.*", SearchOption.AllDirectories).ToList();
                        totalFilesToMove = filesToMove.Count;
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = totalFilesToMove;
                        });

                        foreach (string file in filesToMove)
                        {
                            string newFile = "";

                            if (MainForm_SortInOneFolder.Checked == true)
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "img"), Path.Combine(versionRootFolder, "..", "img"));
                            }
                            else
                            {
                                newFile = file.Replace(Path.Combine(versionRootFolder, lang, "img"), Path.Combine(versionRootFolder, "img"));
                            }

                            if (!Directory.Exists(Path.GetDirectoryName(newFile)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                            }

                            File.Move(file, newFile, true);
                            Invoke((MethodInvoker)delegate ()
                            {
                                MainForm_CurrentTaskProgressBar.Value++;
                            });
                        }

                        Directory.Delete(Path.Combine(versionRootFolder, lang), true);

                        // Une fois le dossier réorganisé, on supprime les fichiers inutiles
                        Invoke((MethodInvoker)delegate ()
                        {
                            MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.CleaningMessage();
                            MainForm_CurrentTaskProgressBar.Value = 0;
                            MainForm_CurrentTaskProgressBar.Maximum = 100;
                            MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Marquee;
                        });
                        File.Delete(Path.Combine(extractDirectory, "COPYRIGHT"));
                        File.Delete(Path.Combine(extractDirectory, "metadata.json"));
                        File.Delete(Path.Combine(extractDirectory, "README.md"));
                        File.Delete(fileName);

                        if (MainForm_SortInOneFolder.Checked == true)
                        {
                            Directory.Delete(extractDirectory, true);
                        }
                    }

                    Invoke((MethodInvoker)delegate ()
                    {
                        MainForm_CurrentVersionProgressBar.Value++;
                    });
                }
            }

            // Updating progress bars, progress bar styles and progress texts
            Invoke((MethodInvoker)delegate ()
            {
                MainForm_CurrentTaskProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentTaskProgressBar.Maximum = 100;
                MainForm_CurrentTaskProgressBar.Value = 100;
                MainForm_CurrentTaskProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_CurrentVersionProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_CurrentVersionProgressBar.Maximum = 100;
                MainForm_CurrentVersionProgressBar.Value = 100;
                MainForm_CurrentVersionProgressBar.Style = ProgressBarStyle.Blocks;
                MainForm_GlobalProgressLabel.Text = TranslationSystem.FinishedMessage();
                MainForm_GlobalProgressBar.Value = 1;
                MainForm_GlobalProgressBar.Style = ProgressBarStyle.Blocks;
            });

            // Reactivating UI
            DownloadFinished();
        }

        private void DownloadFinished()
        {
            Invoke((MethodInvoker)delegate ()
            {
                // Start handling translation for progress texts
                downloadingProcessRunning = false;

                // Unblocking editing of download settings
                MainForm_Settings_DownloadFolder_TextBox.ReadOnly = false;
                MainForm_Settings_DownloadFolder_Browse.Enabled = true;
                MainForm_DownloadModeLight.Enabled = true;
                MainForm_DownloadModeFull.Enabled = true;
                MainForm_DownloadModeExtractOnly.Enabled = true;
                MainForm_DownloadModeGenerateLinksOnly.Enabled = true;
                MainForm_SortInOneFolder.Enabled = true;
                MainForm_SortBySet.Enabled = true;

                // Enabling "Start download" button
                MainForm_Button_StartDownload.Enabled = true;
            });

            // Displaying finished alert
            MessageBox.Show(TranslationSystem.FinishedAlertMessage(MainForm_Settings_DownloadFolder_TextBox.Text));
        }

        private void AutoRefreshForm_Tick(object sender, EventArgs e)
        {
            MainForm_CurrentTaskProgressLabel.Refresh();
            MainForm_CurrentTaskProgressBar.Refresh();
            MainForm_CurrentVersionProgressLabel.Refresh();
            MainForm_CurrentVersionProgressBar.Refresh();
            MainForm_GlobalProgressLabel.Refresh();
            MainForm_GlobalProgressBar.Refresh();
        }

        private void MainForm_Settings_DownloadFolder_Browse_Click(object sender, EventArgs e)
        {
            DownloadFolderSelect.ShowDialog();
            MainForm_Settings_DownloadFolder_TextBox.Text = DownloadFolderSelect.SelectedPath;
        }

        /**
         * Handling language change from UI.
         */
        private void MainForm_LanguageSelector_SelectionChangeCommitted(object sender, EventArgs e)
        {
            switch (MainForm_LanguageSelector.SelectedItem.ToString())
            {
                case "Français":
                    appLanguage = "fr";
                    AppSettings.ChangeAppLanguage(appLanguage);
                    InitFormTexts();
                    break;
                case "English":
                default:
                    appLanguage = "en";
                    AppSettings.ChangeAppLanguage(appLanguage);
                    InitFormTexts();
                    break;
            }
        }

        private void MainForm_DownloadModeGenerateLinksOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (MainForm_DownloadModeGenerateLinksOnly.Checked)
            {
                MainForm_SortInOneFolder.Checked = false;
                MainForm_SortInOneFolder.Enabled = false;
                MainForm_SortBySet.Checked = false;
                MainForm_SortBySet.Enabled = false;
            }
            else
            {
                MainForm_SortInOneFolder.Enabled = true;
                MainForm_SortBySet.Enabled = true;
            }
        }
    }
}
