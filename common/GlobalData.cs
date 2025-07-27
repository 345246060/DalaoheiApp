using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace common
{
    public class ExecutionTask
    {
        public int Id { get; set; } // 任务ID
        public string OuterId { get; set; } // 外部ID
        public string SecretKey { get; set; } // 秘钥
        public string nickname { get; set; } // 达人号名字
        public int AutoCouponInterval { get; set; } // 自动优惠券的间隔时间（以分钟为单位）
        public int coupon_remain_warning { get; set; } // 补券预警值
        public DateTime NextExecutionTime { get; set; } // 下次执行时间

        public ExecutionTask(int id, string outerId, string secretKey, int autoCouponInterval, string _nickname, int _coupon_remain_warning)
        {
            Id = id;
            OuterId = outerId;
            SecretKey = secretKey;
            AutoCouponInterval = autoCouponInterval;
            nickname = _nickname;
            coupon_remain_warning = _coupon_remain_warning;
        }
    }
    public class DeviceQueueManager
    {
        private static readonly DeviceQueueManager _instance = new DeviceQueueManager();
        private readonly ConcurrentQueue<string> _deviceQueue = new ConcurrentQueue<string>();

        // 私有构造函数
        private DeviceQueueManager() { }

        public static DeviceQueueManager Instance => _instance;

        // 加入设备ID
        public void EnqueueDevice(string deviceId)
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                _deviceQueue.Enqueue(deviceId);
            }
        }

        // 尝试取出设备ID
        public bool TryDequeueDevice(out string deviceId)
        {
            return _deviceQueue.TryDequeue(out deviceId);
        }

        // 判断是否有待处理数据
        public bool HasPendingDevices => !_deviceQueue.IsEmpty;
    }
    public class GlobalData
    {
        public static string ip = "172.20.10.10";
        public static string sysfile = "DalaoheiApp";
        public static string sys_name = "晚安玛卡巴卡";
        public static string login_title = "晚安玛卡巴卡";
        public static string Authorization = string.Empty;
        public static string url = $"http://{ip}:8001/";
  
        public static string username { get; set; }
        public static string user_code { get; set; }
        public static string key = "AndHzKTaZWCsrbwC";
        public static string outer_id { get; set; }
        public static string biz_account_id { get; set; }
        public static string userinfo { get; set; }
        public static string msToken { get; set; } 
        public static int randomNumber { get; set; }
        public static bool AreDevToolsEnabled = true;
        public static string nickname { get; set; }
        public static string short_id { get; set; }
        public static string short_img { get; set; }
        public static string deviceId { get; set; }
        public static string iid { get; set; }
        public static string expiration_time { get; set; } 
        public static string rtmp_push_url { get; set; }
        public static string stream_id { get; set; }
        public static string room_id { get; set; }
        public static string localPath { get; set; }
        public static string ua = "Aweme 1.8.0 rv:1.8.0.373809295 (iPhone; iOS 18.1.1; zh-Hans_US) Cronet";
        //DJI Fly/1109 CFNetwork/1568.200.51 Darwin/24.1.0

        /// <summary>
        /// 硬件类型
        /// </summary>
        public static string hardware_type { get; set; }   
        
    }
}
