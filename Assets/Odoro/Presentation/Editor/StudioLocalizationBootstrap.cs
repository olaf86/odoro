using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Odoro.Editor
{
    [InitializeOnLoad]
    internal static class StudioLocalizationBootstrap
    {
        private const string RootFolder = "Assets/Odoro/Presentation/Localization";
        private const string LocalesFolder = RootFolder + "/Locales";
        private const string StringTablesFolder = RootFolder + "/StringTables";
        private const string SettingsPath = RootFolder + "/Studio Localization Settings.asset";

        private static bool ensureScheduled;
        private static bool ensureRunning;

        static StudioLocalizationBootstrap()
        {
            ScheduleEnsureSetup();
        }

        [MenuItem("Odoro/Localization/Sync Studio Tables")]
        private static void SyncStudioTablesMenu()
        {
            EnsureSetup();
            Debug.Log("Odoro localization assets are synchronized.");
        }

        private static void ScheduleEnsureSetup()
        {
            if (ensureScheduled)
            {
                return;
            }

            ensureScheduled = true;
            EditorApplication.delayCall += EnsureSetupOnDelayCall;
        }

        private static void EnsureSetupOnDelayCall()
        {
            ensureScheduled = false;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ScheduleEnsureSetup();
                return;
            }

            EnsureSetup();
        }

        private static void EnsureSetup()
        {
            if (ensureRunning)
            {
                return;
            }

            ensureRunning = true;

            try
            {
                EnsureFolder("Assets/Odoro", "Localization");
                EnsureFolder(RootFolder, "Locales");
                EnsureFolder(RootFolder, "StringTables");

                var settings = EnsureLocalizationSettings();
                if (LocalizationEditorSettings.ActiveLocalizationSettings != settings)
                {
                    LocalizationEditorSettings.ActiveLocalizationSettings = settings;
                    EditorUtility.SetDirty(settings);
                }

                var english = EnsureLocale(SystemLanguage.English, "English");
                var japanese = EnsureLocale(SystemLanguage.Japanese, "Japanese");
                EnsureProjectLocales(english, japanese);
                EnsureProjectLocale(settings, english);

                var collection = EnsureStringTableCollection(new List<Locale> { english, japanese });
                var englishTable = EnsureTable(collection, english);
                var japaneseTable = EnsureTable(collection, japanese);

                var changed = SeedMissingEntries(englishTable, entry => entry.english);
                changed |= SeedMissingEntries(japaneseTable, entry => entry.japanese);

                if (changed)
                {
                    EditorUtility.SetDirty(englishTable.SharedData);
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                ensureRunning = false;
            }
        }

        private static LocalizationSettings EnsureLocalizationSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Odoro Localization Settings";
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static Locale EnsureLocale(SystemLanguage systemLanguage, string assetName)
        {
            var path = $"{LocalesFolder}/{assetName}.asset";
            var locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(systemLanguage);
                AssetDatabase.CreateAsset(locale, path);
            }

            return locale;
        }

        private static void EnsureProjectLocales(params Locale[] locales)
        {
            for (var i = 0; i < locales.Length; i += 1)
            {
                var locale = locales[i];
                var existing = LocalizationEditorSettings.GetLocale(locale.Identifier);
                if (existing == null)
                {
                    LocalizationEditorSettings.AddLocale(locale);
                }
            }
        }

        private static void EnsureProjectLocale(LocalizationSettings settings, Locale locale)
        {
            if (LocalizationSettings.ProjectLocale == locale)
            {
                return;
            }

            LocalizationSettings.ProjectLocale = locale;
            EditorUtility.SetDirty(settings);
        }

        private static StringTableCollection EnsureStringTableCollection(IList<Locale> locales)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(StudioL10n.TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(StudioL10n.TableName, StringTablesFolder, locales);
            }

            return collection;
        }

        private static StringTable EnsureTable(StringTableCollection collection, Locale locale)
        {
            var table = collection.GetTable(locale.Identifier) as StringTable;
            if (table != null)
            {
                return table;
            }

            return collection.AddNewTable(locale.Identifier) as StringTable;
        }

        private static bool SeedMissingEntries(StringTable table, System.Func<StudioLocalizationSeedEntry, string> selector)
        {
            var changed = false;

            for (var i = 0; i < StudioLocalizationSeed.Entries.Length; i += 1)
            {
                var definition = StudioLocalizationSeed.Entries[i];
                var localizedValue = selector(definition);
                var entry = table.GetEntry(definition.key);

                if (entry == null)
                {
                    table.AddEntry(definition.key, localizedValue);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(table);
            }

            return changed;
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            var folderPath = $"{parentFolder}/{folderName}";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
