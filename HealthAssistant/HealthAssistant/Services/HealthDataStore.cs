using HealthAssistant.Models;

namespace HealthAssistant.Services
{
    internal class HealthDataStore : IDataStore<MeasuredItem>
    {
     
        /// <summary>
        /// Every data for the different measurement types is stored in separate files.
        /// </summary>
        /// <param name="Type">Type of measurement to get file name for</param>
        /// <returns></returns>
        private string GetFileNameForType(Measurement Type)
        {
            return Path.Combine(FileSystem.AppDataDirectory, $"{Type}.csv");
        }

        /// <summary>
        /// Convert item data into a ";" separated list
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private string ConvertItemToStorageString(MeasuredItem Item)
        {
            var str = $"{Item.Id};{Item.MeasurementType};{Item.MeasurementDateTime.ToUniversalTime()};";
            if (Item.MeasurementType == Measurement.BloodPressure)
            {
                str += $"{Item.SysValue};{Item.DiaValue};{Item.Unit};";
            }
            else
            {
                str += $"{Item.MeasuredValue};{Item.Unit};";
            }
            return str;
        }

        /// <summary>
        /// Convert the stored data into an object
        /// </summary>
        /// <param name="StoredString">String with the data for the item</param>
        /// <returns></returns>
        private MeasuredItem ConvertStorageStringToItem(string StoredString)
        {
            MeasuredItem Item = new MeasuredItem();
            var splittedString = StoredString.Split(';');
            Item.Id = splittedString[0];
            Item.MeasurementType = (Measurement)Enum.Parse(typeof(Measurement), splittedString[1]);
            Item.MeasurementDateTime = DateTime.Parse(splittedString[2]);

            if (Item.MeasurementType == Measurement.BloodPressure)
            {
                Item.DiaValue = Convert.ToDouble(splittedString[3]);
                Item.SysValue = Convert.ToDouble(splittedString[4]);
            }
            else
            {
                Item.MeasuredValue = Convert.ToDouble(splittedString[3]);
            }
            return Item;
        }

        /// <summary>
        /// Create the data storage (files) for a given type of measurement
        /// and write the first line with the column info
        /// </summary>
        /// <param name="Type">Type of the measurement that is to be stored</param>
        private void CreateDataSourceForType(Measurement Type)
        {
            var fileName = GetFileNameForType(Type);

            if (!File.Exists(fileName))
            {
                using (var stream = File.OpenWrite(fileName))
                {
                    using (var streamWriter = new StreamWriter(stream))
                    {
                        if (Type == Measurement.BloodPressure)
                        {
                            streamWriter.WriteLine("ID;MeasuredType;MeasurmentDateTime;Systolic;Diastolic;Unit;");
                        }
                        else
                        {
                            streamWriter.WriteLine("ID;MeasuredType;MeasurmentDateTime;Measaurement;Unit;");
                        }
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                }
            }
        }

        public HealthDataStore()
        {
            // Create all the files and write 
            CreateDataSourceForType(Measurement.BloodPressure);
            CreateDataSourceForType(Measurement.Glucose);
            CreateDataSourceForType(Measurement.Pulse);
            CreateDataSourceForType(Measurement.Temperature);
            CreateDataSourceForType(Measurement.Weight);
        }

        public async Task<bool> AddItemAsync(MeasuredItem item)
        {
            if (item == null || item.MeasurementType == Measurement.NotSet)
                return await Task.FromResult(false);

            var fileName = GetFileNameForType(item.MeasurementType);
            using (var streamWriter = File.AppendText(fileName))
            {
                item.Id = Guid.NewGuid().ToString();
                await streamWriter.WriteLineAsync(ConvertItemToStorageString(item));
            }
            return await Task.FromResult(true);
        }

        public Task<bool> DeleteItemAsync(MeasuredItem Item)
        {
            throw new NotImplementedException();
        }

        public Task<MeasuredItem> GetItemAsync(string Id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MeasuredItem>> GetItemsAsync(Measurement Type)
        {
            var fileName = GetFileNameForType(Type);
            string[] lines = File.ReadAllLines(fileName);

            var items = new List<MeasuredItem>();
            // the first line is the heading so start at line 1
            for (int i = 1; i < lines.Length; i++)
            {
                items.Add(ConvertStorageStringToItem(lines[i]));
            }
            return items;
        }

        public Task<bool> UpdateItemAsync(MeasuredItem Item)
        {
            throw new NotImplementedException();
        }
    }
}