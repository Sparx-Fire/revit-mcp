// 
//                       RevitAPI-Solutions
// Copyright (c) Duong Tran Quang (DTDucas) (baymax.contact@gmail.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

namespace RevitMCPCommandSet.Utils;

public class DeleteWarningSuperUtils : IFailuresPreprocessor
{
    public int NumberErr;

    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        FailureProcessingResult failureProcessingResult;
        var failList = failuresAccessor.GetFailureMessages();
        if (failList.Count != 0)
        {
            foreach (var failure in failList)
            {
                var s = failure.GetSeverity();
                var failureDefinitionId = failure.GetFailureDefinitionId();

                if (s == FailureSeverity.Warning)
                {
                    if (failureDefinitionId == BuiltInFailures.GeneralFailures.DuplicateValue)
                        failuresAccessor.DeleteWarning(failure);
                    else
                        failuresAccessor.DeleteWarning(failure);
                }
                else if (s == FailureSeverity.Error)
                {
                    failuresAccessor.ResolveFailure(failure);
                    NumberErr += 1;
                }
            }

            failureProcessingResult = FailureProcessingResult.ProceedWithCommit;
        }
        else
        {
            failureProcessingResult = FailureProcessingResult.Continue;
        }

        return failureProcessingResult;
    }
}