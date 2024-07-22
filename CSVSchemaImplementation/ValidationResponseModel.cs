using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVSchemaImplementation
{
    public class ValidationResponseModel
    {
        public bool IsValid { get; set; }
        public List<ValidationResultModel> Results { get; set; }

        public ValidationResponseModel()
        {
            Results = new List<ValidationResultModel>();
        }
    }
}
