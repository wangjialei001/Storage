using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class Enumeration : IComparable
    {
        private readonly int _value;
        private readonly string _displayName;
        public int Value
        {
            get { return _value; }
        }
        public int CompareTo(object other)
        {
            return Value.CompareTo(((Enumeration)other).Value);
        }
        protected Enumeration()
        {
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        protected Enumeration(int value, string displayName)
        {
            _value = value;
            _displayName = displayName;
        }
    }
    public class RequestBackStatuEnum : Enumeration
    {
        private RequestBackStatuEnum(int value, string displayName) : base(value, displayName)
        {

        }
        /// <summary>
        /// 成功执行
        /// </summary>
        public static readonly RequestBackStatuEnum success = new RequestBackStatuEnum(200, nameof(success).ToLowerInvariant());
        /// <summary>
        /// 不存在指定对象
        /// </summary>
        public static readonly RequestBackStatuEnum notexist = new RequestBackStatuEnum(2001, nameof(notexist).ToLowerInvariant());
        /// <summary>
        /// 存在指定对象
        /// </summary>
        public static readonly RequestBackStatuEnum exist = new RequestBackStatuEnum(2002, nameof(exist).ToLowerInvariant());
        /// <summary>
        /// 存在指定条件的数据集
        /// </summary>
        public static readonly RequestBackStatuEnum hasdata = new RequestBackStatuEnum(2003, nameof(hasdata).ToLowerInvariant());
        /// <summary>
        /// 不存在指定条件的数据集
        /// </summary>
        public static readonly RequestBackStatuEnum nodata = new RequestBackStatuEnum(2004, nameof(nodata).ToLowerInvariant());
        /// <summary>
        /// 服务端错误
        /// </summary>
        public static readonly RequestBackStatuEnum fail = new RequestBackStatuEnum(2005, nameof(fail).ToLowerInvariant());
        /// <summary>
        /// 请求失败
        /// </summary>
        public static readonly RequestBackStatuEnum badrequest = new RequestBackStatuEnum(2006, nameof(badrequest).ToLowerInvariant());
        /// <summary>
        /// 未找到指定接口
        /// </summary>
        public static readonly RequestBackStatuEnum notfound = new RequestBackStatuEnum(2007, nameof(notfound).ToLowerInvariant());
        /// <summary>
        /// 网络连接失败
        /// </summary>
        public static readonly RequestBackStatuEnum loselink = new RequestBackStatuEnum(2008, nameof(loselink).ToLowerInvariant());
        /// <summary>
        /// 没有权限
        /// </summary>
        public static readonly RequestBackStatuEnum unauthorized = new RequestBackStatuEnum(401, nameof(unauthorized).ToLowerInvariant());

        public static IEnumerable<RequestBackStatuEnum> List() =>
            new[] { success, badrequest, notfound, loselink };
    }
}
