using System;
using System.Collections.Generic;
using Destrospean.OutfitAssignment.UI.Columns;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.OutfitAssignment.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.OutfitAssignment
{
    public static class InteractionInstanceTypeUtils
    {
        private static Type[] sInteractionInstanceTypes;

        public static Type[] InteractionInstanceTypes
        {
            get
            {
                if (sInteractionInstanceTypes == null)
                {
                    InitInteractionInstanceTypes();
                }
                return sInteractionInstanceTypes;
            }
        }

        public enum CallbackTypes
        {
            Never,
            InteractionStarted,
            InteractionEnded,
            StandardEntry,
            StandardExit,
            SyncLevelRouted,
            SyncLevelCommitted,
            SyncLevelCompleted,
            OutfitChanged
        }

        public static void InitInteractionInstanceTypes()
        {
            var interactionInstanceTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                interactionInstanceTypes.AddRange(Array.FindAll(assembly.GetTypes(), x => typeof(InteractionInstance).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract));
            }
            sInteractionInstanceTypes = interactionInstanceTypes.ToArray();
        }

        public static bool TryGetSelectedInteractionInstanceTypes(out Type[] selectedInteractionInstanceTypes, Type[] allInteractionInstanceTypes = null, string namespaceListTitle = null, string interactionListTitle = null)
        {
            try
            {
                const string interactionListDialogLocalizationPath = Common.kLocalizationPath + "/Dialogs/InteractionListDialog";
                const string namespaceListDialogLocalizationPath = Common.kLocalizationPath + "/Dialogs/NamespaceListDialog";
                allInteractionInstanceTypes = allInteractionInstanceTypes ?? InteractionInstanceTypes;
                Array.Sort(allInteractionInstanceTypes, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
                var namespaces = new List<string>();
                foreach (var interactionInstanceType in allInteractionInstanceTypes)
                {
                    if (!namespaces.Contains(interactionInstanceType.Namespace))
                    {
                        namespaces.Add(interactionInstanceType.Namespace);
                    }
                }

                while (true)
                {
                    var selectedNamespaces = ObjectPickerDialog.Show(namespaceListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(namespaceListDialogLocalizationPath + ":Title"), new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), namespaces.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                        }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                        {
                            new TextColumn(namespaceListDialogLocalizationPath)
                        }, 1, out var confirmed, out var cancelled);
                    if (cancelled)
                    {
                        selectedInteractionInstanceTypes = null;
                        return false;
                    }
                    selectedInteractionInstanceTypes = (ObjectPickerDialog.Show(interactionListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(interactionListDialogLocalizationPath + ":Title"), new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", selectedNamespaces[0], new List<Type>(allInteractionInstanceTypes).FindAll(x => x.Namespace == selectedNamespaces[0]).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                        }, new List<ObjectPickerDialog.CommonHeaderInfo<Type>>
                        {
                            new TypeColumn(interactionListDialogLocalizationPath)
                        }, int.MaxValue, out confirmed, out cancelled) ?? new List<Type>()).ToArray();
                    if (confirmed)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                selectedInteractionInstanceTypes = null;
                return false;
            }
        }
    }
}
