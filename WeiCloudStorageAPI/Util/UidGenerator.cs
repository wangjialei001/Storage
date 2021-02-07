using System;
using System.Collections.Generic;
using System.Text;

namespace WeiCloudStorageAPI.Util
{
    public class UidGenerator
    {
        private static readonly IdWorker1 worker = new IdWorker1(1);
        public static long Uid()
        {
            long id = worker.nextId();
            int maxLen = 16;
            if (id.ToString().Length > maxLen)
            {
                string idStr = id.ToString();
                id = long.Parse(idStr.Substring(idStr.Length - maxLen));
            }
            return id;
        }
        private static readonly IdWorker1 worker1 = new IdWorker1(1);
        public static long Uid(int minute)
        {
            long id = worker1.nextId(minute);
            int maxLen = 16;
            if (id.ToString().Length > maxLen)
            {
                string idStr = id.ToString();
                id = long.Parse(idStr.Substring(idStr.Length - maxLen));
            }
            return id;
        }
    }
    public class IdWorker1
    {
        //机器ID
        private static long workerId;
        private static long twepoch = 687888001020L; //唯一时间，这是一个避免重复的随机量，自行设定不要大于当前时间戳
        private static long sequence = 0L;
        private static int workerIdBits = 4; //机器码字节数。4个字节用来保存机器码(定义为Long类型会出现，最大偏移64位，所以左移64位没有意义)
        public static long maxWorkerId = -1L ^ -1L << workerIdBits; //最大机器ID
        private static int sequenceBits = 5; //计数器字节数，10个字节用来保存计数码
        private static int workerIdShift = sequenceBits; //机器码数据左移位数，就是后面计数器占用的位数
        private static int timestampLeftShift = sequenceBits + workerIdBits; //时间戳左移动位数就是机器码和计数器总字节数
        public static long sequenceMask = -1L ^ -1L << sequenceBits; //一微秒内可以产生计数，如果达到该值则等到下一微妙在进行生成
        private long lastTimestamp = -1L;
        private int minute = 0;
        /// <summary>
        /// 机器码
        /// </summary>
        /// <param name="workerId"></param>
        public IdWorker1(long workerId, int m = 0)
        {
            if (workerId > maxWorkerId || workerId < 0)
                throw new Exception(string.Format("worker Id can't be greater than {0} or less than 0 ", workerId));
            IdWorker1.workerId = workerId;
        }

        public long nextId(int m = 0)
        {
            minute = m;
            lock (this)
            {
                long timestamp = timeGen();
                if (this.lastTimestamp == timestamp)
                { //同一微妙中生成ID
                    IdWorker1.sequence = (IdWorker1.sequence + 1) & IdWorker1.sequenceMask; //用&运算计算该微秒内产生的计数是否已经到达上限
                    if (IdWorker1.sequence == 0)
                    {
                        //一微妙内产生的ID计数已达上限，等待下一微妙
                        timestamp = tillNextMillis(this.lastTimestamp);
                    }
                }
                else
                { //不同微秒生成ID
                    IdWorker1.sequence = 0; //计数清0
                }
                this.lastTimestamp = timestamp; //把当前时间戳保存为最后生成ID的时间戳
                long nextId = (timestamp - twepoch << timestampLeftShift) | IdWorker1.workerId << IdWorker1.workerIdShift | IdWorker1.sequence;
                return nextId;
            }
        }

        /// <summary>
        /// 获取下一微秒时间戳
        /// </summary>
        /// <param name="lastTimestamp"></param>
        /// <returns></returns>
        private long tillNextMillis(long lastTimestamp)
        {
            long timestamp = timeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = timeGen();
            }
            return timestamp;
        }

        /// <summary>
        /// 生成当前时间戳
        /// </summary>
        /// <returns></returns>
        private long timeGen()
        {
            if (minute != 0)
                return (long)(DateTime.UtcNow.AddMinutes(minute) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            else
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
