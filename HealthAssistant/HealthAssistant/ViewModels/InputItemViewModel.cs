using HealthAssistant.Models;

namespace HealthAssistant.ViewModels
{
    public class InputItemViewModel
    {
        public InputItemViewModel()
        {
            Item = new MeasuredItem();
            Item.MeasurementType = Models.Measurement.NotSet;
            MeasuredValue = 0;
            SysValue = 0;
            DiaValue = 0;
            MeasurementDateTime = DateTime.MinValue;
        }

        public MeasuredItem Item { get; private set; }

        public Measurement MeasurementType
        {
            get
            {
                return Item.MeasurementType;
            }

            set
            {
                Item.MeasurementType = value;
            }
        }
        public DateTime MeasurementDateTime
        {
            get
            {
                return Item.MeasurementDateTime;
            }

            set
            {
                Item.MeasurementDateTime = value;
            }
        }

        public double? MeasuredValue
        {
            get
            {
                return Item.MeasuredValue;
            }

            set
            {
                if (value.HasValue)
                {
                    Item.MeasuredValue = value.Value;
                }
            }
        }

        public double? SysValue
        {
            get
            {
                return Item.SysValue;
            }

            set
            {
                if (value.HasValue)
                {
                    Item.SysValue = value.Value;
                }
            }
        }

        public double? DiaValue
        {
            get
            {
                return Item.DiaValue;
            }

            set
            {
                if (value.HasValue)
                {
                    Item.DiaValue = value.Value;
                }
            }
        }

        public MeasurementUnit Unit
        {
            get
            {
                return Item.Unit;
            }

            set
            {
                Item.Unit = value;
            }
        }

        public bool DateIsSet
        {
            get
            {

                return Item.MeasurementDateTime.Date != DateTime.MinValue;
            }
        }
        public bool TimeIsSet
        {
            get
            {
                if (Item.MeasurementDateTime.TimeOfDay == TimeSpan.Zero)
                {
                    return false;
                } else { return true; }
            }
        }
        public bool HasValue
        {
            get
            {
                if ((Item.MeasurementType == Measurement.BloodPressure) && (DiaValue == null) && (SysValue == null))
                    return false;
                if (MeasuredValue == null)
                    return false;
                return true;
            }
        }

        public bool IsComplete
        {
            get
            {
                return HasValue && DateIsSet && TimeIsSet;
            }
        }

    }

}
