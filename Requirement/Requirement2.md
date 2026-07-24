# 需求 2 #
在FileSyncService 获取到所有文件信息之后，做下面的事情：
1. 准备向MES端发送的接口json，结构如下： 
   {
    "docType": "CUST_FILE", //fixed
    "updateType": "UPDATE",//fixed
    "data": [
        {
           "prod_code":"KK70000010",//fixed
           "org_id": "3701",//fixed
           "mtrl_code":"",// 物料编码，就是item_code
            "file_list":[{
               "file_code":"file001", // "File" + 序号，序号是两位
               "file_name":"测试文件",//文件名称
               "remark":"beizhu",//文件注解，暂时为空
               "file_url":"",//RelativePath
               "file_content":"dddddd",//文件的base64
               "file_type":"1" // 暂时都写1
           },{
               "file_code":"file002",// "File" + 序号，序号是两位 
               "file_name":"测试文件",
               "remark":"beizhu",
               "file_url":"",
               "file_content":"dddddd",//文件的base64
               "file_type":"1"
           }]   
        }
    ]
}
一般来说只有一个文件，但是如果发现路径的文件是.zip文件，则需要解压缩，然后把每一个文件都读取，解析成base64，发送。
远端地址http://10.95.6.37:8888/ims-integrate/api/updateImsData