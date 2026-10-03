using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using malafein.Valheim.Shared;
using UnityEngine;

namespace ValheimHotKeys
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.malafein.valheimhotkeys";
        public const string ModName = "Valheim HotKeys";
        public const string ModVersion = "1.4.0";

        public static ConfigEntry<KeyboardShortcut> ToggleHUDConfig;
        public static ConfigEntry<KeyboardShortcut> RepairHammerConfig;
        public static ConfigEntry<KeyboardShortcut>[] HotbarConfigs = new ConfigEntry<KeyboardShortcut>[8];

        private const string ItemBindingsSection = "Custom Item Bindings";
        
        public static System.Collections.Generic.List<ConfigEntry<ItemBinding>> CustomItemBindings = new System.Collections.Generic.List<ConfigEntry<ItemBinding>>();

        private readonly Harmony harmony = new Harmony(ModGUID);
        private static ConfigEntryBase _waitingEntry;
        private static int _activationFrame = -1;
        private static int _forbiddenFrame = -1;

        private void Awake()
        {
            Log.Init(Logger);

            TomlTypeConverter.AddConverter(typeof(ItemBinding), new TypeConverter
            {
                ConvertToObject = (str, type) => ItemBinding.Deserialize(str),
                ConvertToString = (obj, type) => ((ItemBinding)obj).Serialize()
            });

            ToggleHUDConfig = Config.Bind("General", "ToggleHUD", new KeyboardShortcut(KeyCode.F3), "Hotkey to toggle the HUD visibility.");
            RepairHammerConfig = Config.Bind("Actions", "RepairHammer", new KeyboardShortcut(KeyCode.None), "Equips your hammer with repair mode selected. Press again while repairing to put the hammer away.");

            Keybinds.Init(Config);
            Keybinds.Add(ToggleHUDConfig);
            Keybinds.Add(RepairHammerConfig);

            // Valheim's Controls menu gives each hotbar slot one alternate key but can't bind
            // modifiers. Same section and keys as before 1.3.0 removed these, so settings saved
            // back then are picked up again.
            for (int i = 0; i < 8; i++)
            {
                int slotNumber = i + 1;
                var entry = Config.Bind("Hotbar", $"Slot{slotNumber}", new KeyboardShortcut(KeyCode.None), $"Hotkey for hotbar slot {slotNumber}, for shortcuts with modifier keys (e.g. Alt + {slotNumber}). For a single key, use the alternate key in Valheim's own Controls menu.");
                HotbarConfigs[i] = entry;
                Keybinds.Add($"Hotbar slot {slotNumber}", () => entry.Value);
            }

            MigrateSlotKeys();

            for (int i = 0; i < 8; i++)
            {
                int itemNumber = i + 1;
                var attributes = new ConfigurationManagerAttributes { CustomDrawer = DrawItemBindingElement, HideDefaultButton = false };
                
                ItemBinding defaultBinding = new ItemBinding();
                if (i == 0)
                {
                    defaultBinding.ItemName = "Healing Mead";
                    defaultBinding.Shortcut = new KeyboardShortcut(KeyCode.Mouse3);
                }
                else if (i == 1)
                {
                    defaultBinding.ItemName = "Arrow";
                    defaultBinding.Shortcut = new KeyboardShortcut(KeyCode.Mouse4);
                }

                var entry = Config.Bind(ItemBindingsSection, $"Item {itemNumber}", defaultBinding, new ConfigDescription($"Custom item binding {itemNumber}.", null, attributes));
                CustomItemBindings.Add(entry);

                // A binding with no item name never fires, so its key can't conflict.
                Keybinds.Add($"Item {itemNumber}", () => string.IsNullOrEmpty(entry.Value.ItemName) ? KeyboardShortcut.Empty : entry.Value.Shortcut);
            }

            Log.Info($"{ModName} {ModVersion} is loading...");
            harmony.PatchAll();
            Log.Info($"{ModName} loaded!");
        }

        // Up to 1.3.0 the item bindings were named "Slot 1" to "Slot 8", which read like hotbar
        // slots. BepInEx keeps settings it has no binding for in a private orphan table and
        // loads a key's value from there when it's bound, so moving each saved value to its new
        // key before binding carries it over. The old key is dropped on the next save.
        private void MigrateSlotKeys()
        {
            try
            {
                var orphans = (Dictionary<ConfigDefinition, string>)AccessTools
                    .Property(typeof(ConfigFile), "OrphanedEntries")
                    .GetValue(Config, null);

                for (int itemNumber = 1; itemNumber <= 8; itemNumber++)
                {
                    var oldKey = new ConfigDefinition(ItemBindingsSection, $"Slot {itemNumber}");
                    var newKey = new ConfigDefinition(ItemBindingsSection, $"Item {itemNumber}");
                    if (!orphans.TryGetValue(oldKey, out string value)) continue;

                    orphans.Remove(oldKey);
                    if (!orphans.ContainsKey(newKey)) orphans[newKey] = value;
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Could not carry over item bindings saved by an older version: {e.Message}");
            }
        }

        private void DrawItemBindingElement(ConfigEntryBase entry)
        {
            var bindingEntry = (ConfigEntry<ItemBinding>)entry;
            ItemBinding binding = bindingEntry.Value;

            GUILayout.BeginHorizontal();
            
            // Item Name field
            string newName = GUILayout.TextField(binding.ItemName, GUILayout.Width(200));
            if (newName != binding.ItemName)
            {
                // IMMUTABLE UPDATE: Create a new object so BepInEx "Reset" recognizes the change
                bindingEntry.Value = new ItemBinding { ItemName = newName, Shortcut = binding.Shortcut };
            }

            GUILayout.Space(10);

            // Shortcut button
            bool isWaiting = _waitingEntry == entry;
            
            GUI.enabled = !isWaiting;
            string shortcutText = isWaiting ? "Press any key..." : Keybinds.Format(binding.Shortcut);
            if (!isWaiting && binding.Shortcut.MainKey == KeyCode.None) shortcutText = "Click to bind";

            if (GUILayout.Button(shortcutText, GUILayout.Width(150)))
            {
                _waitingEntry = entry;
                _activationFrame = Time.frameCount;
            }
            GUI.enabled = true;

            if (isWaiting)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                {
                    _waitingEntry = null;
                    _forbiddenFrame = Time.frameCount; // Cooldown for Clear button
                    Event.current.Use();
                }
                else
                {
                    Event e = Event.current;
                    
                    if (Time.frameCount > _activationFrame && (e.type == EventType.KeyDown || e.type == EventType.MouseDown || e.type == EventType.Used))
                    {
                        if (e.type != EventType.Used && (e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.ScrollWheel))
                        {
                            // Ignore noise
                        }
                        else
                        {
                            KeyCode capturedKey = KeyCode.None;
                            if (e.keyCode != KeyCode.None) capturedKey = e.keyCode;
                            // Valheim 1.0's ZInput only resolves Mouse0-Mouse4, so binding
                            // a higher mouse button would capture a key that never fires.
                            else if (e.button >= 0 && e.button <= 4) capturedKey = (KeyCode)((int)KeyCode.Mouse0 + e.button);

                            // IGNORE Mouse0 (Left Click) to prevent interference with UI buttons
                            if (capturedKey != KeyCode.None && capturedKey != KeyCode.Mouse0)
                            {
                                if (capturedKey == KeyCode.Escape)
                                {
                                    _waitingEntry = null;
                                    e.Use();
                                }
                                else if (capturedKey != KeyCode.LeftControl && capturedKey != KeyCode.RightControl &&
                                         capturedKey != KeyCode.LeftShift && capturedKey != KeyCode.RightShift &&
                                         capturedKey != KeyCode.LeftAlt && capturedKey != KeyCode.RightAlt &&
                                         capturedKey != KeyCode.LeftCommand && capturedKey != KeyCode.RightCommand)
                                {
                                    // Read the physical keys rather than e.control/e.shift/e.alt,
                                    // which can't tell left from right.
                                    KeyCode[] modifiers = Keybinds.HeldModifiers();

                                    // IMMUTABLE UPDATE
                                    bindingEntry.Value = new ItemBinding { ItemName = binding.ItemName, Shortcut = new KeyboardShortcut(capturedKey, modifiers) };
                                    _waitingEntry = null;
                                    e.Use();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Clear button with Forbidden Frame cooldown
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    if (Time.frameCount != _forbiddenFrame)
                    {
                        bindingEntry.Value = new ItemBinding { ItemName = "", Shortcut = KeyboardShortcut.Empty };
                        Event.current.Use();
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        public class ItemBinding
        {
            public string ItemName = "";
            public KeyboardShortcut Shortcut = KeyboardShortcut.Empty;

            public string Serialize()
            {
                if (string.IsNullOrEmpty(ItemName)) return "";
                return $"{ItemName}|{Shortcut.Serialize()}";
            }

            public static ItemBinding Deserialize(string str)
            {
                var result = new ItemBinding();
                if (string.IsNullOrEmpty(str)) return result;

                int separatorIndex = str.LastIndexOf('|');
                if (separatorIndex == -1)
                {
                    result.ItemName = str;
                    return result;
                }

                result.ItemName = str.Substring(0, separatorIndex);
                string shortcutStr = str.Substring(separatorIndex + 1);
                
                try
                {
                    result.Shortcut = KeyboardShortcut.Deserialize(shortcutStr);
                }
                catch
                {
                    result.Shortcut = KeyboardShortcut.Empty;
                }

                return result;
            }
        }

        // Helper class for Configuration Manager
        public class ConfigurationManagerAttributes
        {
            public System.Action<ConfigEntryBase> CustomDrawer;
            public bool? ShowMultilineText;
            public string DispName;
            public int? Order;
            public bool? HideDefaultButton;
            public bool? HideSettingName;
        }
    }
}
