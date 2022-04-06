using System;

namespace HealthAssistant.Models
{

    public enum Intent
    {
        BloodPressure,
        Glucose,
        Pulse,
        Temperature,
        Cancel,
        None
    };
    public enum Measurement
    {
        BloodPressure,
        Glucose,
        Pulse,
        Temperature,
        Weight,
        NotSet
    };
    public class MeasuredItem
    {
        public  MeasuredItem()
        {
            MeasurementType = Models.Measurement.NotSet;
            MeasuredValue = 0;
            SysValue = 0;
            DiaValue = 0;
            MeasurementDateTime = DateTime.MinValue;
        }
        public string Id { get; set; }
        public Measurement MeasurementType { get; set; }
        public DateTime MeasurementDateTime { get; set; }
        public double MeasuredValue { get; set; }
        public double SysValue { get; set; }
        public double DiaValue { get; set; }
        public string Unit 
        { 
            get
            {
                switch (MeasurementType)
                {
                    case Measurement.BloodPressure:
                        return "mmHg";
                    case Measurement.Glucose:
                        return "ml/DL";
                    case Measurement.Pulse:
                        return "bpm";
                    case Measurement.Temperature:
                        return "°C";
                    case Measurement.Weight:
                        return "kg";
                    case Measurement.NotSet:
                        return "";
                    default:
                        return "";
                }
            }
        }

    }
}