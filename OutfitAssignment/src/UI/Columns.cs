using System;
using System.Collections.Generic;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.OutfitAssignment.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.OutfitAssignment.UI.Columns
{
    public class BodyTypeColumn : ObjectPickerDialog.CommonHeaderInfo<BodyTypes>
    {
        private readonly string mLocalizationPath;

        public BodyTypeColumn(string localizationPath) : base(localizationPath + "/Headers/PartType:Text", localizationPath + "/Headers/PartType:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(BodyTypes bodyType)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/PartType:" + bodyType));
        }
    }

    public class CASAgeGenderFlagColumn : ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>
    {
        private readonly string mLocalizationPath;

        public CASAgeGenderFlagColumn(string localizationPath) : base(localizationPath + "/Headers/CASAgeGenderFlag:Text", localizationPath + "/Headers/CASAgeGenderFlag:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(CASAgeGenderFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/CASAgeGenderFlag:" + flag));
        }
    }

    public class CASAgeGenderFlagEnabledColumn : ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>
    {
        private readonly string mLocalizationPath;

        private readonly List<CASAgeGenderFlags> mTraitsEnabled;

        public CASAgeGenderFlagEnabledColumn(string localizationPath, CASAgeGenderFlags[] flagsEnabled) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mTraitsEnabled = new List<CASAgeGenderFlags>(flagsEnabled);
        }

        public override ObjectPicker.ColumnInfo GetValue(CASAgeGenderFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mTraitsEnabled.Contains(flag)));
        }
    }

    public class PartOverrideEnabledColumn : ObjectPickerDialog.CommonHeaderInfo<BodyTypes>
    {
        private readonly string mLocalizationPath;

        private readonly List<BodyTypes> mPartOverrides;

        public PartOverrideEnabledColumn(string localizationPath, BodyTypes[] partOverrides) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mPartOverrides = new List<BodyTypes>(partOverrides);
        }

        public override ObjectPicker.ColumnInfo GetValue(BodyTypes bodyType)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mPartOverrides.Contains(bodyType)));
        }
    }

    public class TextColumn : ObjectPickerDialog.CommonHeaderInfo<string>
    {
        public TextColumn(string localizationPath) : base(localizationPath + "/Header:Text", localizationPath + "/Header:Tooltip", 440)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(string text)
        {
            return new ObjectPicker.TextColumn(text ?? "");
        }
    }

    public class TypeColumn : ObjectPickerDialog.CommonHeaderInfo<Type>
    {
        public TypeColumn(string localizationPath) : base(localizationPath + "/Header:Text", localizationPath + "/Header:Tooltip", 440)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(Type type)
        {
            return new ObjectPicker.TextColumn(type == null ? "" : type.FullName);
        }
    }
}
