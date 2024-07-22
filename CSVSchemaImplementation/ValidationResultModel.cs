using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVSchemaImplementation
{
    public class ValidationResultModel
    {
        public int LineNumber { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string Column { get; set; }

        public ValidationResultModel(int lineNumber, bool isValid, string column, string message)
        {
            LineNumber = lineNumber;
            IsValid = isValid;
            Column = column;
            Message = message;
        }
    }
}