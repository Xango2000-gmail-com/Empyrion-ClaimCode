using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ClaimCode
{
    class Insurance
    {
        public static Root ReadYaml(string filePath)
        {
            using (var input = File.OpenText(filePath))
            {
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                var Output = deserializer.Deserialize<Root>(input);
                return Output;
            }
        }

        public class Root
        {
            public List<InsuredEntity> InsuredEntities { get; set; }
        }

        public class InsuredEntity
        {
            public int EntityID { get; set; }
            public string BlueprintName { get; set; }
            public string InitialSpawnDate { get; set; }
            public int InsuranceRemaining { get; set; }
        }

        public static void WriteYaml(string Path, Root ConfigData)
        {
            File.WriteAllText(Path, "---\r\n");
            Serializer serializer = new SerializerBuilder().Build();
            string WriteThis = serializer.Serialize(ConfigData);
            File.AppendAllText(Path, WriteThis);
        }
    }
}