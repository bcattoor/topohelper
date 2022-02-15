using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Autocad
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Infrabel.AutodeskPlatform.AutoCADCommon.Extensions
    {
        /// <summary>
        /// Add fuctionality for working with ActionExtentions in autocad.
        /// </summary>
        public static class ActionExtentions
        {
            /// <exception cref="ArgumentNullException">
            /// previousUcsName is <see langword="null" />
            /// </exception>
            /// <exception cref="InvalidOperationException">
            /// We could not create the UCS table record.
            /// </exception>
            /// <exception cref="Exception">
            /// A delegate callback throws an exception.
            /// </exception>
            public static void WrapInWorldUcs(this Action action)
            {
                const string previousName = "TopoHelper_previous";

                MdiActiveDocumentInteractions.SetWcs(previousName);

                var ac = action;
                if (ac != null)
                {
                    //! EDIT BC 25/11/2013: We should invoke in same threat
                    //? As a side note, it is generally preferable to take a local copy
                    //? of a class level delegate before invoking it to avoid a race condition
                    //? whereby OnAdd is not null at the time that it is checked,
                    //? but is at the time that it is invoked:
                    ac();
                }

                MdiActiveDocumentInteractions.SetWcs(previousName, true);
            }
        }
    }
}