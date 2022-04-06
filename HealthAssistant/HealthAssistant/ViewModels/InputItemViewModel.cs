using CommunityToolkit.Mvvm.ComponentModel;
using HealthAssistant.Models;

namespace HealthAssistant.ViewModels
{
    public class InputItemViewModel: ObservableObject
    {
        public InputItemViewModel()
        {
            Item = new MeasuredItem();
            Item.MeasurementType = Models.Measurement.NotSet;
            MeasuredValue = null;
            SysValue = null;
            DiaValue = null;
            MeasurementDateTime = DateTime.MinValue;
        }

        public InputItemViewModel(MeasuredItem MeasureItem)
        {
            Item = MeasureItem;
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
                if (value.HasValue && value.Value > 0)
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
                if (value.HasValue && value.Value > 0)
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
                if (value.HasValue && value.Value > 0) 
                {
                    Item.DiaValue = value.Value;
                }
            }
        }

        public string Unit
        {
            get
            {
                return Item.Unit;
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
                if (Item.MeasurementType == Measurement.BloodPressure)
                {
                    if ((SysValue == null) || (DiaValue == null))
                    {
                        return false;
                    }
                    else
                    {
                        if ((SysValue <= 0) || (DiaValue <= 0))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                }
                else
                {
                    if (MeasuredValue == null)
                    {
                        return false;
                    }
                    else
                    {
                        if (MeasuredValue <= 0)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
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
