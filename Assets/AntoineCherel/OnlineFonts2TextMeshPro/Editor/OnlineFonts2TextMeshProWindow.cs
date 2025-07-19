using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using System.IO;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;
using OnlineFonts2TextMeshPro.Core;
using System;
using System.Linq;
using UnityEngine.TextCore.Text;


namespace OnlineFonts2TextMeshPro
{
    public class OnlineFonts2TextMeshProWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTree;


        private const string GOOGLE_FONTS_DOWNLOAD_URL = "fonts.google.com/download/list?family=";
        private const string FONT_SQUIRREL_DOWNLOAD_URL = "https://www.fontsquirrel.com/fonts/download/";
        private const string FONTSHARE_DOWNLOAD_URL = "https://api.fontshare.com/v2/fonts/download/";


        private const string TTF_EXTENSION = ".ttf";
        private const string OTF_EXTENSION = ".otf";

        private const string SETTINGS_ROOT_FOLDER_KEY = "GF2TMP_ROOT_FOLDER";
        private const string SETTINGS_FONT_ASSET_PREFIX_KEY = "GF2TMP_FONT_ASSET_PREFIX";
        private const string SETTINGS_FONT_ASSET_SUFFIX_KEY = "GF2TMP_FONT_ASSET_SUFFIX";
        private const string SETTINGS_DOWNLOAD_STATIC_FOLDER_KEY = "GF2TMP_STATIC_FOLDER";
        private const string SETTINGS_CREATE_UI_TOOLKIT_ASSETS_KEY = "GF2TMP_CREATE_UI_TOOLKIT_ASSETS";
        private const string SETTINGS_OPEN_FOLDER_AFTER_DOWNLOAD_KEY = "GF2TMP_OPEN_FOLDER_AFTER_DOWNLOAD";


        private Button openGoogleFontsButton;
        private Button openFontSquirrelButton;
        private Button openFontshareButton;
        private Button openDocumentationButton;
        private Button closeToolButton;

        // Settings
        private Foldout settingsFoldout;
        private TextField settingsTestPhraseTextField;
        private TextField settingsFontsRootFolderTextField;
        private Button settingsFontsRootFolderCreateButton;
        private TextField settingsFontAssetPrefixTextField;
        private TextField settingsFontAssetSuffixTextField;
        private Label settingsFontAssetExample;
        private Toggle settingsDownloadStaticFolderToggle;
        private Toggle settingsCreateUIToolkitFontAssetToggle;
        private Toggle settingsOpenFolderAfterDownloadToggle;

        // Download
        private VisualElement downloadVisualElement;

        private Foldout googleFontsFoldout;
        private TextField googleFontsTextField;
        private Button downloadGoogleFontsButton;


        private Foldout fontSquirrelFoldout;
        private TextField fontSquirrelTextField;
        private Button downloadFontSquirrelButton;
        

        private Foldout fontshareFoldout;
        private TextField fontshareTextField;
        private Button downloadFontshareButton;

        private Foldout[] downloaderFoldouts;

        private ProgressBar downloadProgressBar;
        private Label downloadFontPreviewLabel;

        private EditorCoroutine downloadDoneCoroutine;



        [MenuItem("Tools/Online fonts 2 TextMesh Pro")]
        public static void OpenOnlineFonts2TextMeshProWindow()
        {
            OnlineFonts2TextMeshProWindow wnd = GetWindow<OnlineFonts2TextMeshProWindow>();
            wnd.titleContent = new GUIContent("Online fonts 2 TextMeshPro");
        }

        public void CreateGUI()
        {

            visualTree.CloneTree(rootVisualElement);


            openDocumentationButton = rootVisualElement.Q<Button>("OpenDocumentationButton");
            closeToolButton = rootVisualElement.Q<Button>("CloseToolButton");

            openGoogleFontsButton = rootVisualElement.Q<Button>("OpenGoogleFontsButton");
            openFontSquirrelButton = rootVisualElement.Q<Button>("OpenFontSquirrelButton");
            openFontshareButton = rootVisualElement.Q<Button>("OpenFontshareButton");

            // Settings 
            settingsFoldout = rootVisualElement.Q<Foldout>("Settings");
            settingsTestPhraseTextField = rootVisualElement.Q<TextField>("SettingsTestPhraseTextField");
            settingsFontsRootFolderTextField = rootVisualElement.Q<TextField>("SettingsFontsRootFolderTextField");
            settingsFontsRootFolderCreateButton = rootVisualElement.Q<Button>("SettingsFontsRootFolderCreateButton");
            settingsFontAssetPrefixTextField = rootVisualElement.Q<TextField>("SettingsTMPFontAssetPrefixTextField");
            settingsFontAssetSuffixTextField = rootVisualElement.Q<TextField>("SettingsTMPFontAssetSuffixTextField");
            settingsFontAssetExample = rootVisualElement.Q<Label>("SettingsTMPFontAssetExampleLabel");
            settingsDownloadStaticFolderToggle = rootVisualElement.Q<Toggle>("SettingsDownloadStaticToggle");
            settingsCreateUIToolkitFontAssetToggle = rootVisualElement.Q<Toggle>("SettingsCreateUIToolkitFontAssetToggle");
            settingsOpenFolderAfterDownloadToggle = rootVisualElement.Q<Toggle>("SettingsOpenFolderAfterDownloadToggle");

            // Downloader
            downloadVisualElement = rootVisualElement.Q<VisualElement>("Downloader");

            googleFontsFoldout = rootVisualElement.Q<Foldout>("GoogleFontsFoldout");
            googleFontsTextField = rootVisualElement.Q<TextField>("DownloadGoogleFontsTextField");
            downloadGoogleFontsButton = rootVisualElement.Q<Button>("DownloadGoogleFontsButton");


            fontSquirrelFoldout = rootVisualElement.Q<Foldout>("FontSquirrelFoldout");
            fontSquirrelTextField = rootVisualElement.Q<TextField>("DownloadFontSquirrelTextField");
            downloadFontSquirrelButton = rootVisualElement.Q<Button>("DownloadFontSquirrelButton");

            fontshareFoldout = rootVisualElement.Q<Foldout>("FontshareFoldout");
            fontshareTextField = rootVisualElement.Q<TextField>("DownloadFontshareTextField");
            downloadFontshareButton = rootVisualElement.Q<Button>("DownloadFontshareButton");

            downloaderFoldouts = new Foldout[] { googleFontsFoldout, fontSquirrelFoldout, fontshareFoldout };

            downloadProgressBar = rootVisualElement.Q<ProgressBar>("DownloadProgressBar");
            downloadFontPreviewLabel = rootVisualElement.Q<Label>("DownloadFontPreviewLabel");


            openDocumentationButton.clicked += OpenDocumentation;
            closeToolButton.clicked += Close;

            googleFontsFoldout.RegisterValueChangedCallback(UpdateDownloaderFoldouts);
            fontSquirrelFoldout.RegisterValueChangedCallback(UpdateDownloaderFoldouts);
            fontshareFoldout.RegisterValueChangedCallback(UpdateDownloaderFoldouts);

            openGoogleFontsButton.clicked += OpenGoogleFonts;
            openFontSquirrelButton.clicked += OpenFontSquirrel;
            openFontshareButton.clicked += OpenFontshare;

            downloadGoogleFontsButton.clicked += TryDownloadGoogleFonts;
            downloadFontSquirrelButton.clicked += TryDownloadFontSquirrel;
            downloadFontshareButton.clicked += TryDownloadFontshare;


            settingsFontsRootFolderCreateButton.clicked += TryCreateFontsRootFolder;

            settingsTestPhraseTextField.RegisterValueChangedCallback(UpdatePreviewTestPhrase);
            settingsFontsRootFolderTextField.RegisterValueChangedCallback(EvaluateFontsRootFolderPath);
            settingsFontAssetPrefixTextField.RegisterValueChangedCallback(FontAssetPrefixChanged);
            settingsFontAssetSuffixTextField.RegisterValueChangedCallback(FontAssetSuffixChanged);
            settingsCreateUIToolkitFontAssetToggle.RegisterValueChangedCallback(CreateUIToolkitSettingChanged);
            settingsDownloadStaticFolderToggle.RegisterValueChangedCallback(DownloadStaticFolderSettingChanged);
            settingsOpenFolderAfterDownloadToggle.RegisterValueChangedCallback(OpenFolderAfterDownloadSettingChanged);


            if (EditorPrefs.HasKey(SETTINGS_ROOT_FOLDER_KEY))
            {
                settingsFontsRootFolderTextField.SetValueWithoutNotify(EditorPrefs.GetString(SETTINGS_ROOT_FOLDER_KEY));
            }
            if (EditorPrefs.HasKey(SETTINGS_FONT_ASSET_PREFIX_KEY))
            {
                settingsFontAssetPrefixTextField.SetValueWithoutNotify(EditorPrefs.GetString(SETTINGS_FONT_ASSET_PREFIX_KEY));
            }
            if (EditorPrefs.HasKey(SETTINGS_FONT_ASSET_SUFFIX_KEY))
            {
                settingsFontAssetSuffixTextField.SetValueWithoutNotify(EditorPrefs.GetString(SETTINGS_FONT_ASSET_SUFFIX_KEY));
            }
            if (EditorPrefs.HasKey(SETTINGS_DOWNLOAD_STATIC_FOLDER_KEY))
            {
                settingsDownloadStaticFolderToggle.SetValueWithoutNotify(EditorPrefs.GetBool(SETTINGS_DOWNLOAD_STATIC_FOLDER_KEY));
            }
            if (EditorPrefs.HasKey(SETTINGS_CREATE_UI_TOOLKIT_ASSETS_KEY))
            {
                settingsCreateUIToolkitFontAssetToggle.SetValueWithoutNotify(EditorPrefs.GetBool(SETTINGS_CREATE_UI_TOOLKIT_ASSETS_KEY));
            }
            if (EditorPrefs.HasKey(SETTINGS_OPEN_FOLDER_AFTER_DOWNLOAD_KEY))
            {
                settingsOpenFolderAfterDownloadToggle.SetValueWithoutNotify(EditorPrefs.GetBool(SETTINGS_OPEN_FOLDER_AFTER_DOWNLOAD_KEY));
            }


            EvaluateFontsRootFolderPath();
        }

        private void UpdateDownloaderFoldouts(ChangeEvent<bool> evt)
        {
            foreach (Foldout foldout in downloaderFoldouts)
            {
                if (foldout != evt.target)
                {
                    foldout.SetValueWithoutNotify(false);
                }
            }
        }

        private void UpdatePreviewTestPhrase(ChangeEvent<string> evt)
        {
            downloadFontPreviewLabel.text = evt.newValue;
        }

        #region Settings
        private void OpenFolderAfterDownloadSettingChanged(ChangeEvent<bool> evt)
        {
            EditorPrefs.SetBool(SETTINGS_OPEN_FOLDER_AFTER_DOWNLOAD_KEY, evt.newValue);
        }

        private void DownloadStaticFolderSettingChanged(ChangeEvent<bool> evt)
        {
            EditorPrefs.SetBool(SETTINGS_DOWNLOAD_STATIC_FOLDER_KEY, evt.newValue);
        }

        private void CreateUIToolkitSettingChanged(ChangeEvent<bool> evt)
        {
            EditorPrefs.SetBool(SETTINGS_CREATE_UI_TOOLKIT_ASSETS_KEY, evt.newValue);
        }
        private void TryCreateFontsRootFolder()
        {
            if (!Directory.Exists(GetFontsRootFolderPath()))
            {
                Directory.CreateDirectory(GetFontsRootFolderPath());
                settingsFontsRootFolderCreateButton.style.display = DisplayStyle.None;
                AssetDatabase.Refresh();
            }
            else
            {
                LogError("This folder already exists");
            }
        }

        private void EvaluateFontsRootFolderPath(ChangeEvent<string> evt)
        {
            EvaluateFontsRootFolderPath();

            EditorPrefs.SetString(SETTINGS_ROOT_FOLDER_KEY, evt.newValue);
        }

        private void EvaluateFontsRootFolderPath()
        {
            if (Directory.Exists(GetFontsRootFolderPath()))
            {
                settingsFontsRootFolderCreateButton.style.display = DisplayStyle.None;
            }
            else
            {
                settingsFontsRootFolderCreateButton.style.display = DisplayStyle.Flex;
            }
        }

        private void FontAssetSuffixChanged(ChangeEvent<string> evt)
        {
            settingsFontAssetExample.text = GetTMPAssetFileName("Poiret_One-Regular") + ".asset";
            EditorPrefs.SetString(SETTINGS_FONT_ASSET_SUFFIX_KEY, evt.newValue);
        }

        private void FontAssetPrefixChanged(ChangeEvent<string> evt)
        {
            settingsFontAssetExample.text = GetTMPAssetFileName("Poiret_One-Regular") + ".asset";
            EditorPrefs.SetString(SETTINGS_FONT_ASSET_PREFIX_KEY, evt.newValue);
        }
        #endregion


        private void OpenFontSquirrel()
        {
            Application.OpenURL("https://www.fontsquirrel.com/");
        }

        private void OpenGoogleFonts()
        {
            Application.OpenURL("https://fonts.google.com/");
        }

        private void OpenFontshare()
        {
            Application.OpenURL("https://www.fontshare.com/");
        }

        private void OpenDocumentation()
        {
            string localPath = Application.dataPath + "/AntoineCherel/OnlineFonts2TextMeshPro/Documentation/documentation.html";

            if (File.Exists(localPath))
            {
                Application.OpenURL("file:///" + localPath);
            }
            else
            {
                Application.OpenURL("https://www.antoinecherel.dev/online-fonts-2-text-mesh-pro");
            }
        }

        private void TryDownloadFontshare()
        {
            if (!IsFontsRootFolderValid())
            {
                settingsFoldout.value = true;
                LogError("Please input a valid Fonts root folder");
                return;
            }

            if (string.IsNullOrEmpty(fontshareTextField.value))
            {
                LogError("Please input a valid Font name");
                return;
            }

            if (Uri.IsWellFormedUriString(fontshareTextField.value, UriKind.Absolute) &&
                Uri.TryCreate(fontshareTextField.value, UriKind.Absolute, out Uri uri) &&
                TryFindFontName(uri, "fonts", out string fontName))
            {
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(FONTSHARE_DOWNLOAD_URL, fontName, ExtractFontshareFontFamily), this);
            }
            else
            {
                string encodedName = fontshareTextField.value.Replace(" ", "-").ToLower();
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(FONTSHARE_DOWNLOAD_URL, encodedName, ExtractFontshareFontFamily), this);
            }
        }


        private void TryDownloadGoogleFonts()
        {
            if (!IsFontsRootFolderValid())
            {
                settingsFoldout.value = true;
                LogError("Please input a valid Fonts root folder");
                return;
            }

            if (string.IsNullOrEmpty(googleFontsTextField.value))
            {
                LogError("Please input a valid Font name");
                return;
            }

            if (Uri.IsWellFormedUriString(googleFontsTextField.value, UriKind.Absolute) &&
                Uri.TryCreate(googleFontsTextField.value, UriKind.Absolute, out Uri uri) &&
                TryFindFontName(uri, "specimen", out string fontName))
            {
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(GOOGLE_FONTS_DOWNLOAD_URL, fontName, ExtractGoogleFontsFontFamily), this);
            }
            else
            {
                string encodedName = fontSquirrelTextField.value.Replace(" ", "+");
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(GOOGLE_FONTS_DOWNLOAD_URL, encodedName, ExtractGoogleFontsFontFamily), this);
            }
        }

        private void TryDownloadFontSquirrel()
        {
            if (!IsFontsRootFolderValid())
            {
                settingsFoldout.value = true;
                LogError("Please input a valid Fonts root folder");
                return;
            }

            if (string.IsNullOrEmpty(fontSquirrelTextField.value))
            {
                LogError("Please input a valid Font name");
                return;
            }

            if (Uri.IsWellFormedUriString(fontSquirrelTextField.value, UriKind.Absolute) &&
                Uri.TryCreate(fontSquirrelTextField.value, UriKind.Absolute, out Uri uri) &&
                TryFindFontName(uri, "fonts", out string fontName))
            {
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(FONT_SQUIRREL_DOWNLOAD_URL, fontName, ExtractFontSquirrelFontFamily), this);
            }
            else
            {
                string encodedName = fontSquirrelTextField.value.Replace(" ", "-").ToLower();
                EditorCoroutineUtility.StartCoroutine(DownloadFontFamilyCoroutine(FONT_SQUIRREL_DOWNLOAD_URL, encodedName, ExtractFontSquirrelFontFamily), this);
            }
        }

        private bool TryFindFontName(Uri uri, string penultimateSlug, out string fontName)
        {
            if (uri.Segments.Length > 0)
            {
                bool penultimateSegmentFound = false;

                foreach (string segment in uri.Segments)
                {
                    if (penultimateSegmentFound)
                    {
                        fontName = segment;
                        return true;
                    }

                    if (string.Equals(penultimateSlug, segment))
                    {
                        penultimateSegmentFound = true;
                    }
                }

                fontName = uri.Segments[uri.Segments.Length - 1];
                return true;
            }
            fontName = string.Empty;
            return false;
        }


        IEnumerator DownloadFontFamilyCoroutine(string baseUrl, string name, Action<DownloadHandler, string> extractingFunction)
        {
            EnableDownloader(false);
            string downloadUrl = baseUrl + name;
            LogMessage("Trying to download " + downloadUrl);
            downloadProgressBar.SetValueWithoutNotify(0);


            UnityWebRequest www = UnityWebRequest.Get(downloadUrl);
            DownloadHandler handle = www.downloadHandler;

            //Send Request and wait
            www.SendWebRequest();
            while (!www.isDone)
            {
                int progressPercentage = (int)(www.downloadProgress * 100f);

                downloadProgressBar.SetValueWithoutNotify(progressPercentage);
                downloadProgressBar.title = "Downloading... " + progressPercentage + "%";

                yield return null;

            }

            if (handle.data == null || !string.IsNullOrEmpty(handle.error))
            {
                downloadProgressBar.title = "Couldn't download font family";
                downloadProgressBar.SetValueWithoutNotify(0);
                EnableDownloader(true);
                LogError(handle.error);
            }
            else
            {
                downloadProgressBar.title = "Extracting";
                downloadProgressBar.SetValueWithoutNotify(100);

                extractingFunction?.Invoke(handle, name);
            }

            www.Dispose();
        }

        private void TryStoppingDownloadDoneCoroutine()
        {
            if (downloadDoneCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(downloadDoneCoroutine);
                downloadDoneCoroutine = null;
            }
        }

        private void EnableDownloader(bool enabled)
        {
            if (!enabled)
            {
                TryStoppingDownloadDoneCoroutine();
            }

            downloadVisualElement.SetEnabled(enabled);
            settingsFoldout.SetEnabled(enabled);
        }

        private bool CheckLocalFolder(string localFolder)
        {
            if (Directory.Exists(localFolder))
            {
                LogError("Looks like you already downloaded this font !");

                downloadProgressBar.title = "Canceled";
                EnableDownloader(true);
                downloadProgressBar.SetValueWithoutNotify(0);
                return true;
            }
            return false;
        }

        private void ExtractFontshareFontFamily(DownloadHandler downloadHandler, string fileName)
        {
            string destinationFolder = GetFontsRootFolderPath();

            string localFolder = destinationFolder + fileName;

            if (CheckLocalFolder(localFolder))
            {
                return;
            }

            Directory.CreateDirectory(localFolder);

            string zipPath = localFolder + "/" + fileName + ".zip";
            File.WriteAllBytes(zipPath, downloadHandler.data);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, localFolder);
            File.Delete(zipPath);

            CreateAllTextMeshProFontAssets(localFolder, TTF_EXTENSION, OTF_EXTENSION);
        }

        private void ExtractFontSquirrelFontFamily(DownloadHandler downloadHandler, string fileName)
        {
            string destinationFolder = GetFontsRootFolderPath();

            string localFolder = destinationFolder + fileName;

            if (CheckLocalFolder(localFolder))
            {
                return;
            }

            Directory.CreateDirectory(localFolder);

            string zipPath = localFolder + "/" + fileName + ".zip";
            File.WriteAllBytes(zipPath, downloadHandler.data);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, localFolder);
            File.Delete(zipPath);

            CreateAllTextMeshProFontAssets(localFolder, TTF_EXTENSION, OTF_EXTENSION);
        }

        private void ExtractGoogleFontsFontFamily(DownloadHandler downloadHandler, string _)
        {
            string destinationFolder = GetFontsRootFolderPath();

            string jsonResponse = downloadHandler.text.Substring(4);

            GoogleFontsJsonResponse response = JsonUtility.FromJson<GoogleFontsJsonResponse>(jsonResponse);

            string fileName = Path.GetFileNameWithoutExtension(response.zipName);
            string localFolder = destinationFolder + fileName;

            if (CheckLocalFolder(localFolder))
            {
                return;
            }

            Directory.CreateDirectory(localFolder);

            foreach (GoogleFontsJsonFile file in response.manifest.files)
            {
                File.WriteAllText(localFolder + "/" + file.filename, file.contents);
            }

            EditorCoroutineUtility.StartCoroutine(DownloadAllGooogleFileRefs(response.manifest.fileRefs, localFolder), this);
        }

        IEnumerator DownloadAllGooogleFileRefs(GoogleFontsJsonFileRefs[] refs, string folder)
        {
            foreach (GoogleFontsJsonFileRefs fileRef in refs)
            {
                string fileName = "";
                List<string> subfolders = new List<string>(fileRef.filename.Split('/'));

                // TODO: refactor to use Uri ??
                // Uri fileRefUri = new Uri(fileRef.filename);

                if (subfolders.Count > 0)
                {
                    fileName = new string(subfolders[subfolders.Count - 1]);
                    subfolders.RemoveAt(subfolders.Count - 1);
                }


                if (!settingsDownloadStaticFolderToggle.value && subfolders.Count > 0)
                {
                    downloadProgressBar.title = "Skipping " + fileRef.filename;
                    continue;
                }

                UnityWebRequest www = UnityWebRequest.Get(fileRef.url);
                DownloadHandler handle = www.downloadHandler;

                //Send Request and wait
                www.SendWebRequest();
                while (!www.isDone)
                {
                    int progressPercentage = (int)(www.downloadProgress * 100f);

                    downloadProgressBar.SetValueWithoutNotify(progressPercentage);
                    downloadProgressBar.title = "Downloading " + fileRef.filename;

                    yield return null;
                }

                if (handle.data == null)
                {
                    downloadProgressBar.title = "Couldn't download font";
                    downloadProgressBar.SetValueWithoutNotify(0);
                    LogError(handle.error);
                }
                else
                {
                    downloadProgressBar.title = "Saving " + fileName;
                    downloadProgressBar.SetValueWithoutNotify(100);

                    string finalPath = new string(folder);

                    foreach (string subfolder in subfolders)
                    {
                        if (!Directory.Exists(finalPath + "/" + subfolder))
                        {
                            Directory.CreateDirectory(finalPath + "/" + subfolder);
                        }

                        finalPath += "/" + subfolder;
                    }

                    File.WriteAllBytes(finalPath + "/" + fileName, handle.data);
                }

                www.Dispose();
            }

            CreateAllTextMeshProFontAssets(folder, TTF_EXTENSION);
        }

        private void CreateAllTextMeshProFontAssets(string folder, params string[] extensions)
        {
            AssetDatabase.Refresh();
            UnityEngine.Object firstObject = null;

            List<Font> downloadedFonts = new List<Font>();

            foreach (string filePath in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).Where(s => extensions.Contains(Path.GetExtension(s).ToLower())))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                Font font = new Font(filePath);

                if (font != null)
                {
                    TMP_FontAsset tmpFontAsset = TMP_FontAsset.CreateFontAsset(font);

                    tmpFontAsset.name = fileName;

                    string folderPath = filePath.Replace(Path.GetFileName(filePath), "");
                    string localFolder = folderPath.Replace(Application.dataPath, "Assets/");

                    AssetDatabase.CreateAsset(tmpFontAsset, localFolder + "/" + GetTMPAssetFileName(tmpFontAsset.name) + ".asset");

                    if (settingsCreateUIToolkitFontAssetToggle.value)
                    {
                        FontAsset fontAsset = FontAsset.CreateFontAsset(font);
                        fontAsset.name = fileName;
                        AssetDatabase.CreateAsset(fontAsset, localFolder + "/" + GetTMPAssetFileName(fontAsset.name) + ".asset");
                    }

                    if (firstObject == null)
                    {
                        firstObject = tmpFontAsset;
                    }

                    downloadedFonts.Add(font);
                }
                else
                {
                    LogError("Couldn't load font '" + fileName + "'. Skipping");
                    continue;
                }
            }

            if (firstObject != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = firstObject;
            }

            downloadProgressBar.title = "Done (" + downloadedFonts.Count + " fonts downloaded)";
            EnableDownloader(true);

            if (settingsOpenFolderAfterDownloadToggle.value)
            {
                Application.OpenURL("file:///" + folder);
            }

            TryStoppingDownloadDoneCoroutine();

            if (downloadedFonts.Count > 0)
            {
                downloadDoneCoroutine = EditorCoroutineUtility.StartCoroutine(DownloadDoneCoroutine(downloadedFonts), this);
            }
        }

        IEnumerator DownloadDoneCoroutine(List<Font> donwloadedFonts)
        {
            int fontIndex = 0;

            while (true)
            {
                Font font = donwloadedFonts[fontIndex];
                downloadFontPreviewLabel.style.unityFontDefinition = new StyleFontDefinition(font);
                yield return new WaitForSecondsRealtime(1.8f);
                fontIndex = (fontIndex + 1) % donwloadedFonts.Count;
            }
        }

        private bool IsFontsRootFolderValid()
        {
            if (string.IsNullOrEmpty(settingsFontsRootFolderTextField.value))
            {
                return false;
            }

            return Directory.Exists(GetFontsRootFolderPath());
        }

        private string GetFontsRootFolderPath()
        {
            string rootFolder = settingsFontsRootFolderTextField.value;
            string potentialPath = Application.dataPath;

            if (rootFolder.Length > 0)
            {
                if (rootFolder[0] != '/')
                {
                    potentialPath += '/';
                }

                potentialPath += rootFolder;

                if (potentialPath[potentialPath.Length - 1] != '/')
                {
                    potentialPath += '/';
                }
            }

            return potentialPath;
        }

        private string GetTMPAssetFileName(string rawFileName)
        {
            string FormatString(string value)
            {
                return Path.GetInvalidFileNameChars().Aggregate(value, (f, c) => f.Replace(c, '_'));
            }

            return FormatString(settingsFontAssetPrefixTextField.value) + rawFileName + FormatString(settingsFontAssetSuffixTextField.value);
        }

        #region Logs
        private void LogError(string errorMessage)
        {
            Debug.LogError("[OnlineFonts2TextMeshPro] " + errorMessage);
        }
        private void LogMessage(string infoMessage)
        {
            Debug.Log("[OnlineFonts2TextMeshPro] " + infoMessage);
        }
        #endregion
    }
}
