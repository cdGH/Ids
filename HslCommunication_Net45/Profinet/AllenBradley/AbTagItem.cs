using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.AllenBradley
{
    /// <summary>
    /// AB PLC的每个的数据标签情况
    /// </summary>
    public class AbTagItem
    {
        /// <summary>
        /// 实例ID
        /// </summary>
        public uint InstanceID { get; set; }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// TAG类型（有时候可作为实例ID使用）
        /// </summary>
        public ushort SymbolType 
        { 
            get => symbolType;
            set {
                symbolType = value;
                ArrayDimension = (symbolType & 0x4000) == 0x4000 ? 2 : 0 + (symbolType & 0x2000) == 0x2000 ? 1 : 0;
                IsStruct = (symbolType & 0x8000) == 0x8000;
            }
        }

        /// <summary>
        /// 数据的维度信息，默认是0，标量数据，1代表1为数组
        /// </summary>
        public int ArrayDimension { get; set; }

        /// <summary>
        /// 是否结构体数据
        /// </summary>
        public bool IsStruct { get; set; }

        private ushort symbolType = 0;
    }
}
