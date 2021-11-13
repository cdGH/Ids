using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.AllenBradley
{
    /// <summary>
    /// 结构体的句柄信息
    /// </summary>
    public class AbStructHandle
    {
        /// <summary>
        /// 返回项数
        /// </summary>
        public ushort Count { get; set; }

        /// <summary>
        /// 结构体定义大小
        /// </summary>
        public uint TemplateObjectDefinitionSize { get; set; }

        /// <summary>
        /// 使用读取标记服务读取结构时在线路上传输的字节数
        /// </summary>
        public uint TemplateStructureSize { get; set; }

        /// <summary>
        /// 成员数量
        /// </summary>
        public ushort MemberCount { get; set; }

        /// <summary>
        /// 结构体的handle
        /// </summary>
        public ushort StructureHandle { get; set; }
    }
}
