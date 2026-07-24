using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomGUIControls.Grid
{
    public class GridFilterItem
    {
        public enum MatchType{Extact,Contains};
        internal GridFilterItem(string filterText, MatchType match, string multiValueSeparator)
        {
            m_filterText = filterText;
            m_match = match;
            m_multiValueSeparator = multiValueSeparator;
        }

        public string FilterText { get { return m_filterText; } }
        public MatchType Match { get { return m_match; } }

        private string m_filterText;
        private MatchType m_match;
        private string m_multiValueSeparator;

    }
}
