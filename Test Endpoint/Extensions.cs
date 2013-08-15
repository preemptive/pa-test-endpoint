// Copyright (c) 2013 PreEmptive Solutions; All Right Reserved, http://www.preemptive.com/
//
// This source is subject to the Microsoft Public License (MS-PL).
// Please see the License.txt file for more information.
// All other rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using System.Collections.Generic;
using System.Linq;

namespace Test_Endpoint
{
    public static class Extensions
    {
        public static string GetValue(this IEnumerable<KeyValuePair<string, string>> list, string key)
        {
            return list.SingleOrDefault(x => x.Key.ToLower() == key.ToLower()).Value;
        }
    }
}
