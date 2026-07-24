# PLM -> MES -> SAP互通工程 #
## 数据库以及配置 ## 
#### 1. 数据库类型 ORACLE DB12 ####
#### 2. 登录信息：####
- 地址： 127.0.0.1
- 连接类型: SID 
- SID： agile9
- Port: 1521
- 用户: agile
- 密码：tartan   
#### 3. 数据表、结构、含义 ####  
1. Item主表 
```SQL 
create table ITEM
(
    ID                  NUMBER(10)                not null
        constraint ITEM_PK
            primary key,
    CLASS               NUMBER(10),
    SUBCLASS            NUMBER(10),
    ITEM_NUMBER         VARCHAR2(300 char)        not null,
    CATEGORY            NUMBER(10),
    DESCRIPTION         VARCHAR2(720 char),
    DOCSIZE             NUMBER(10),
    MODIFYDATE          DATE,
    OBJVERSION          NUMBER,
    DELETE_FLAG         NUMBER,
    PRODUCT_LINES       VARCHAR2(1024 char),
    FLAGS               VARCHAR2(32 char),
    DEFAULT_CHANGE      NUMBER    default 0,
    COMMODITY           NUMBER,
    ENCODE_NAME         VARCHAR2(300 char)        not null
        constraint ITEM_ENCODE_NAME
            unique,
    PART_FAMILY         NUMBER(10),
    CONV_FACTOR         NUMBER(16, 4),
    IS_TLA              NUMBER(1),
    EXCLUDE_FROM_ROLLUP NUMBER(1) default 0,
    ITEM_GROUP          VARCHAR2(765 char),
    LATEST_RELEASED_ECO NUMBER    default 0       not null,
    MODEL_REF           VARCHAR2(40 char),
    CREATED             DATE      default SYSDATE not null,
    LAST_UPD            DATE      default SYSDATE not null,
    FUNC_TEAM           VARCHAR2(765 char)
)
```


2. BOM主表
```SQL
create table BOM
(
    ID                    NUMBER(10)                        not null
        constraint BOM_PK
            primary key,
    ITEM                  NUMBER(10),
    ITEM_NUMBER           VARCHAR2(300 char),
    FIND_NUMBER           VARCHAR2(32 char),
    SEQ                   NUMBER(5),
    QUANTITY              VARCHAR2(40 char),
    DESCRIPTION           VARCHAR2(4000 char),
    NOTES                 VARCHAR2(1333 char),
    DATE01                DATE,
    DATE02                DATE,
    DATE03                DATE,
    DATE04                DATE,
    DATE05                DATE,
    TEXT01                VARCHAR2(150 char),
    TEXT02                VARCHAR2(150 char),
    TEXT03                VARCHAR2(150 char),
    TEXT04                VARCHAR2(150 char),
    TEXT05                VARCHAR2(150 char),
    LIST01                NUMBER(10),
    LIST02                NUMBER(10),
    LIST03                NUMBER(10),
    LIST04                NUMBER(10),
    LIST05                NUMBER(10),
    LIST06                NUMBER(10),
    LIST07                NUMBER(10),
    LIST08                NUMBER(10),
    LIST09                NUMBER(10),
    LIST10                NUMBER(10),
    CHANGE_IN             NUMBER(10),
    CHANGE_OUT            NUMBER(10),
    PRIOR_BOM             NUMBER(10),
    FLAGS                 VARCHAR2(32 char) default '00000000000000000000000000000000',
    COMPONENT             NUMBER            default 0,
    SITE                  NUMBER            default 0,
    NUMERIC01             NUMBER,
    NUMERIC02             NUMBER,
    NUMERIC03             NUMBER,
    NUMERIC04             NUMBER,
    NUMERIC05             NUMBER,
    CREATED               DATE              default SYSDATE not null,
    LAST_UPD              DATE              default SYSDATE not null,
    IS_OPTIONAL           NUMBER(1),
    IS_MUTUALLY_EXCLUSIVE NUMBER(1),
    MINIMUM_NUMBER        NUMBER,
    MAXIMUM_NUMBER        NUMBER,
    TEXT06                VARCHAR2(150 char),
    TEXT07                VARCHAR2(150 char),
    TEXT08                VARCHAR2(150 char),
    TEXT09                VARCHAR2(150 char),
    TEXT10                VARCHAR2(150 char),
    TEXT11                VARCHAR2(150 char),
    TEXT12                VARCHAR2(150 char),
    TEXT13                VARCHAR2(150 char),
    TEXT14                VARCHAR2(150 char),
    TEXT15                VARCHAR2(150 char),
    LIST11                NUMBER(10),
    LIST12                NUMBER(10),
    LIST13                NUMBER(10),
    LIST14                NUMBER(10),
    LIST15                NUMBER(10),
    NUMERIC06             NUMBER,
    NUMERIC07             NUMBER,
    NUMERIC08             NUMBER,
    NUMERIC09             NUMBER,
    NUMERIC10             NUMBER,
    NUMERIC11             NUMBER,
    NUMERIC12             NUMBER,
    NUMERIC13             NUMBER,
    NUMERIC14             NUMBER,
    NUMERIC15             NUMBER,
    DATE06                DATE,
    DATE07                DATE,
    DATE08                DATE,
    DATE09                DATE,
    DATE10                DATE,
    DATE11                DATE,
    DATE12                DATE,
    DATE13                DATE,
    DATE14                DATE,
    DATE15                DATE,
    MULTILIST01           VARCHAR2(765 char),
    MULTILIST02           VARCHAR2(765 char),
    MULTILIST03           VARCHAR2(765 char),
    MULTILIST04           VARCHAR2(765 char),
    MULTILIST05           VARCHAR2(765 char),
    MULTILIST06           VARCHAR2(765 char),
    MULTILIST07           VARCHAR2(765 char),
    MULTILIST08           VARCHAR2(765 char),
    MULTILIST09           VARCHAR2(765 char),
    MULTILIST10           VARCHAR2(765 char)
)
```

3. 贴片资料表 
```SQL 
create table REFDESIG
(
    ID       NUMBER(10)           not null
        constraint REFDESIG_PK
            primary key,
    BOM      NUMBER(10),
    LABEL    VARCHAR2(40 char),
    CREATED  DATE default SYSDATE not null,
    LAST_UPD DATE default SYSDATE not null
)
```
4. Rev表 
```SQL
create table REV
(
    ID                   NUMBER(10)             not null
        constraint REV_PK
            primary key,
    ITEM                 NUMBER(10),
    CHANGE               NUMBER(10),
    REV_NUMBER           VARCHAR2(40 char),
    OLD_REVNUMBER        VARCHAR2(40 char),
    OBSOLETE_DATE        DATE,
    EFFECTIVE_DATE       DATE,
    FUNCTION_ID          NUMBER(10),
    INCORP_DATE          DATE,
    RELEASE_TYPE         NUMBER(10),
    LOC01                NUMBER(10),
    LOC02                NUMBER(10),
    LOC03                NUMBER(10),
    LOC04                NUMBER(10),
    LOC05                NUMBER(10),
    LOC06                NUMBER(10),
    LOC07                NUMBER(10),
    LOC08                NUMBER(10),
    LOC09                NUMBER(10),
    LOC10                NUMBER(10),
    DATE01               DATE,
    DATE02               DATE,
    DATE03               DATE,
    DATE04               DATE,
    DATE05               DATE,
    TEXT01               VARCHAR2(150 char),
    TEXT02               VARCHAR2(150 char),
    TEXT03               VARCHAR2(150 char),
    TEXT04               VARCHAR2(150 char),
    TEXT05               VARCHAR2(150 char),
    LIST01               NUMBER(10),
    LIST02               NUMBER(10),
    LIST03               NUMBER(10),
    LIST04               NUMBER(10),
    LIST05               NUMBER(10),
    LIST06               NUMBER(10),
    LIST07               NUMBER(10),
    LIST08               NUMBER(10),
    LIST09               NUMBER(10),
    LIST10               NUMBER(10),
    RELEASED             NUMBER(5),
    INCORPORATED         NUMBER(5),
    LATEST_FLAG          NUMBER(5),
    RELEASE_DATE         DATE,
    DATE06               DATE,
    DATE07               DATE,
    DATE08               DATE,
    DATE09               DATE,
    DATE10               DATE,
    DATE11               DATE,
    DATE12               DATE,
    DATE13               DATE,
    DATE14               DATE,
    DATE15               DATE,
    DATE16               DATE,
    DATE17               DATE,
    DATE18               DATE,
    DATE19               DATE,
    DATE20               DATE,
    LIST11               NUMBER(10),
    LIST12               NUMBER(10),
    LIST13               NUMBER(10),
    LIST14               NUMBER(10),
    LIST15               NUMBER(10),
    LIST16               NUMBER(10),
    LIST17               NUMBER(10),
    LIST18               NUMBER(10),
    LIST19               NUMBER(10),
    LIST20               NUMBER(10),
    LIST21               NUMBER(10),
    LIST22               NUMBER(10),
    LIST23               NUMBER(10),
    LIST24               NUMBER(10),
    LIST25               NUMBER(10),
    FLAGS                VARCHAR2(32 char),
    TEXT06               VARCHAR2(150 char),
    TEXT07               VARCHAR2(150 char),
    TEXT08               VARCHAR2(150 char),
    TEXT09               VARCHAR2(150 char),
    TEXT10               VARCHAR2(150 char),
    TEXT11               VARCHAR2(150 char),
    TEXT12               VARCHAR2(150 char),
    TEXT13               VARCHAR2(150 char),
    TEXT14               VARCHAR2(150 char),
    TEXT15               VARCHAR2(150 char),
    OLD_RELEASE_TYPE     NUMBER(10),
    SITE                 NUMBER default 0,
    NUMERIC01            NUMBER,
    NUMERIC02            NUMBER,
    NUMERIC03            NUMBER,
    NUMERIC04            NUMBER,
    NUMERIC05            NUMBER,
    DESCRIPTION          VARCHAR2(720 char),
    MONEYVALUE01         NUMBER(18, 6),
    MONEYVALUE02         NUMBER(18, 6),
    MONEYVALUE03         NUMBER(18, 6),
    MONEYVALUE04         NUMBER(18, 6),
    MONEYVALUE05         NUMBER(18, 6),
    MONEYCURRENCY01      NUMBER(4),
    MONEYCURRENCY02      NUMBER(4),
    MONEYCURRENCY03      NUMBER(4),
    MONEYCURRENCY04      NUMBER(4),
    MONEYCURRENCY05      NUMBER(4),
    WEIGHT               NUMBER,
    NORMALIZED_WEIGHT    NUMBER,
    WEIGHT_UOM           NUMBER(10),
    NOTES01              VARCHAR2(1500 char),
    OLD_DESCRIPTION      VARCHAR2(720 char),
    COMPLIANCY           NUMBER(1),
    COMPLIANCY_CALC_DATE DATE,
    AI_MULTILIST01       VARCHAR2(255 char),
    AI_MULTILIST02       VARCHAR2(255 char),
    AI_MULTILIST03       VARCHAR2(255 char),
    AI_MULTILIST04       VARCHAR2(255 char),
    AI_MULTILIST05       VARCHAR2(255 char),
    AI_MULTILIST06       VARCHAR2(255 char),
    AI_MULTILIST07       VARCHAR2(255 char),
    AI_MULTILIST08       VARCHAR2(255 char),
    AI_MULTILIST09       VARCHAR2(255 char),
    AI_MULTILIST10       VARCHAR2(255 char),
    AI_MULTILIST11       VARCHAR2(255 char),
    AI_MULTILIST12       VARCHAR2(255 char),
    AI_MULTILIST13       VARCHAR2(255 char),
    AI_MULTILIST14       VARCHAR2(255 char),
    AI_MULTILIST15       VARCHAR2(255 char),
    CREATED              DATE   default SYSDATE not null,
    LAST_UPD             DATE   default SYSDATE not null,
    FOLDER_OWNER         VARCHAR2(255 char),
    VERSION_ID           NUMBER(10),
    ATTACHMENT           NUMBER(10)
)
``` 
5. Change 表：
```SQL
create table CHANGE
(
    ID                 NUMBER(10)         not null
        constraint CHANGE_PK
            primary key,
    CLASS              NUMBER(10),
    SUBCLASS           NUMBER(10),
    CHANGE_NUMBER      VARCHAR2(90 char),
    CATEGORY           NUMBER(10),
    STATUS             NUMBER(10),
    REASON_CODE        NUMBER(10),
    ORIGINATOR         NUMBER(10),
    OWNER              NUMBER(10),
    CREATE_DATE        DATE,
    RESUME_DATE        DATE,
    EFFECTIVE_FROM     DATE,
    EFFECTIVE_TO       DATE,
    RELEASE_DATE       DATE,
    DESCRIPTION        VARCHAR2(4000 char),
    REASON             VARCHAR2(4000 char),
    TRANSFERRED        VARCHAR2(128 char),
    MODIFYDATE         DATE,
    OBJVERSION         NUMBER,
    DELETE_FLAG        NUMBER,
    SUBMIT_DATE        DATE,
    ROUTE_DATE         DATE,
    PRODUCT_LINES      VARCHAR2(255 char),
    FLAGS              VARCHAR2(32 char),
    WORKFLOW_ID        NUMBER,
    STATUSTYPE         NUMBER,
    FINALCOMPLETE_DATE DATE,
    PROCESS_ID         NUMBER,
    IN_REVIEW          NUMBER,
    ENCODE_NAME        VARCHAR2(300 char) not null
        constraint CHANGE_ENCODE_NAME
            unique,
    FUNC_TEAM          VARCHAR2(765 char),
    ROUTED_DATE        DATE
)
```
6. ListEntry表 
```SQL
create table LISTENTRY
(
    PARENTID     NUMBER,
    ACTIVE       NUMBER,
    ENTRYID      NUMBER,
    ENTRYVALUE   VARCHAR2(2048 char),
    LANGID       NUMBER                    not null,
    DESCRIPTION  VARCHAR2(2048 char),
    PARENT_ENTRY NUMBER,
    ID           NUMBER    default 0       not null,
    APINAME      VARCHAR2(2048 char),
    READONLY     NUMBER(1) default 0,
    CREATED      DATE      default SYSDATE not null,
    LAST_UPD     DATE      default SYSDATE not null
)
```


上面这些表的关系结构(部分)如下：  
1. BOM和Item之间的关系如下：  
ITEM 可以认为是物料表，BOM可以认为是物料的BOM表，其中BOM表里面的item字段 就是ITEM表里面的主键。Item表里面的ITEM_NUMBER是物料的物料编号  
所以BOM表里面同一个Item字段代表的就是同一个物料编号下面的bom。所有bom会有排序，排序是通过的"FIND_NUMBER" 里面应该是根据数字（Integer）来排序  

2. BOM子条目和贴片资料表的关系：  
REFDESIG 可以认为是BOM的插件资料，每一个BOM可以有多个REFDESIG，他们之间以逗号组合起来就是最终的值。  
REFDESIG中的BOM就是BOM里面的Id值。

3. Rev表、Item表与Change表之间的关系：
Item表中有DEFAULT_CHANGE，通过这个值在change表里面找到change的编号（Change_Number），这个Default_Change在Rev里面可以找到当前这个Item的最新Rev， 就是通过Item + Change两个字段查REV_Number就是当前最新版本Rev。 里面还有历史Rev，Rev 的版本的格式类似"R" + 数字，数字越大版本越新，当然也可以从Release_Date字段来判断。

4. ListEntry表的功能和与BOM表里面的关系：
ListEntry是列表枚举值表，里面有很多枚举值。不过这次需求，我们只要用里面的替代料部分的枚举值。具体如下：  
在BOM中List06字段代表替代组别，List07代表在这个替代组别里面的替代优先级。  
两个相同的替代组别的物料是可以互相替代的，替代的优先级顺序按照他所在的ListEntry里面值来比对大小。  
ListEntry里面的EntryId就是替代组别和替代顺序的Id，但是只有该表中的LangId = 4 的时候才起效。比如，ListEntry里面有EntryId = 3807526 两个数据，但是只有LangId同时=4的时候才有效。

5. BOM与Change之间的版本物料异同判断：  
在BOM表里面有两个字段 分别是Change_Out和 Change_In， 这两个字段对应的是Change表里面的主键，你可以通过BOM该行里面的(Change_in 或 Change_Out) + Item 参照Rev + Item  + Change之间的关系查到改行物料在哪个Rev移除，在哪个Rev移入。 移除之后如果没有后续的移入，则这个物料在后续版本就不会出现。如果一个物料在加入后没有显示移除，则后续所有的版本他都会在。 

6. BOM 有 Rev之间每一条记录之间的关联：
BOM里面的每一个子条目都可以通过Component去Rev查版本，通过Bom里面的Component作为条件查Rev里面的Item，查出来的Rev_Number就是有效的版本，但是要取最新的Release_Date字段，或者Latest_FLAG为1的时候就是最新的版本。
Rev表里面的对应的DESCRIPTION就是该条BOM里面的描述
最后输出的网页里面能看到的log files 里面要显示这个每一条子项目的版本和描述，现在已经有描述了，直接把描述填入成这个就行


现在在oracle中，已经做好了触发器 。BOM表在新增、删除、修改之后会有触发器，触发器会向127.0.0.1 31456发送POST，里面字段如下：
1. BOM 表插入：
```JSON
{"id":1,"action":"INSERT","table":"BOM"}

```
2. BOM 表更新：
```JSON
{"id":2,"action":"UPDATE","table":"BOM"}
```

3. BOM 表删除：
```JSON
{"id":3,"action":"DELETE","table":"BOM"}
```



#### 4. 打包数据上传到系统2（MES） ####
通过post方式上传到接口地址：http://10.95.6.37:8888/ims-integrate/api/updateImsData
内容如下
{
    "docType": "BS_BOM",
    "updateType": "UPDATE",
    "data": [
        {
            "org_code": "5410",
            "prod_code":"mtrl_code001",
            "bom_ver":"v1.0",
            "is_def":"y",
            "is_valid":"y",
            "remark":"测试",
            "bs_bom_mtrl": [
                {
                    "mtrl_code":"0-1280004-2",
                    "is_main":"y",
                    "main_code":"0-1280004-2",
                    "dosage":4,
                    "point_str":"U1,U2,U3,U4",
                    "mbom_ver":null,
                    "remark":"测试"
                },
                {
                    "mtrl_code":"0-1280005-1",
                    "is_main":"n",
                    "main_code":"0-1280013-7",
                    "dosage":3,
                    "point_str":"U1,U2,U3",
                    "mbom_ver":null,
                    "remark":"测试"
                },
                {
                    "mtrl_code":"0-1280013-7",
                    "is_main":"y",
                    "main_code":"0-1280013-7",
                    "dosage":2,
                    "mbom_ver":null,
                    "point_str":"U1,U2",
                    "remark":"测试"
                },
                {
                    "mtrl_code":"1-1280006-1",
                    "is_main":"y",
                    "main_code":"1-1280006-1",
                    "dosage":3,
                    "point_str":"U1,U2,U3",
                    "mbom_ver":null,
                    "remark":"测试"
                }
            ]
        }
    ]
}

编号	字段说明	字段描述	数据类型	是否必填	备注
1	org_code	组织编码	VARCHAR2(40)	是	
2	prod_code	产品编码	VARCHAR2(40)	是	
3	bom_ver	BOM版本	VARCHAR2(50)	是	
4	is_def	默认	VARCHAR2(1)	是	
5	is_valid	启用状态	VARCHAR2(1)	是	
6	bs_bom_mtrl	bom物料明细			
7	mtrl_code	物料编码	VARCHAR2(40)	是	
8	is_main	是否主料	VARCHAR2(1)	是	
9	main_code	主料编码	VARCHAR2(40)	是	
10	dosage	用量	nUMBER(20,6)	是	
11	mbom_ver	BOM版本	VARCHAR2(50)	否	
12	point_str	点位	VARCHAR2(2000)	否	


说明：
1. org_code 现在默认写3701
2. prod_code  就是产品的item编号
3. bom_ver 就是产品的最新的版本
4. is_def 默认写'y' 
5. is_valid 默认写'y'
6. bs_bom_mtrl 是一个数组，里面的含义：
    6.1 mtrl_code 子bom单行的物料编码
    6.2 is_main 如果SubGrp  没有值，则默认填写'y'，如果SubGrp  有值，则查看SubPri ，如果SubPri 为1，则是'y'否则是'n'
    6.3 main_code 如果SubGrp  没有值，则默认填写mtrl_code一样的值，如果SubGrp  有值，则查看SubPri ，如果SubPri 为1，则是默认填写mtrl_code一样的值，否则是该同一个组的SubGrp的SubPri为1 的那一行的mtrl_code
          产品下的物料明细编码，是否主料，如果是主料 物料编码=主料编码，如果不是主料，物料编码=替代料编码，主料编码=主（物料编码）
   6.4 dosage 就是用量Qty 
   6.5 mbom_ver 是改行物料的版本'Rx'
   6.6 point_str 是位号RefDesig           
传输给MES系统的时候，只要传输第一阶的数据就行，无需穿透传输（既无需传输大于第一层的BOM）






#### 5. 制作Windows服务 ####  
 4.1 做一个31456的webAPI，监听到内容后，如果里面的table字段是BOM，则开始开启定时器，记录这个Id，大约15秒之后开始执行下面的操作，不过收到后立刻回复成功（200）  
 - 构建Item - BOM表，然后每一条BOM里面要拼接好贴片资料， 要该物料的所有版本（所有Rev）下面的内容，内容要按照版本区分显示。
 - 把上面的这个输出到日志里面，日志就在本服务所在的目录下的log

4.2 使用技术栈：C# .net core 9.0 EF Core  做成一个Windows服务
4.3 外部看板：能看到触发记录、同步记录、日志文件等等要详细。要美观，详细，现代化。
4.4 验收标准： 上面这些都能完成



#### 6. File Sync 工程 #### 
1. 现有触发器：  
``` SQL
CREATE OR REPLACE PROCEDURE send_files_change(p_id IN NUMBER, p_action IN VARCHAR2) IS
  PRAGMA AUTONOMOUS_TRANSACTION;
  v_req        UTL_HTTP.REQ;
  v_resp       UTL_HTTP.RESP;
  v_url        VARCHAR2(200) := 'http://127.0.0.1:31456/';
  v_post_data  VARCHAR2(500);
BEGIN
  v_post_data := '{"id":' || p_id || ',"action":"' || p_action || '","table":"FILES"}';

  v_req := UTL_HTTP.BEGIN_REQUEST(url => v_url, method => 'POST', http_version => 'HTTP/1.1');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Type', 'application/json; charset=UTF-8');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Length', LENGTHB(v_post_data));
  UTL_HTTP.WRITE_TEXT(v_req, v_post_data);

  v_resp := UTL_HTTP.GET_RESPONSE(v_req);
  BEGIN
    UTL_HTTP.END_RESPONSE(v_resp);
  EXCEPTION
    WHEN OTHERS THEN NULL;
  END;

  COMMIT;
EXCEPTION
  WHEN OTHERS THEN
    ROLLBACK;
END send_files_change;
/


CREATE OR REPLACE TRIGGER trg_files_change
  AFTER INSERT OR UPDATE OR DELETE ON FILES
  FOR EACH ROW
BEGIN
  IF INSERTING THEN
    send_files_change(:NEW.ID, 'INSERT');
  ELSIF UPDATING THEN
    send_files_change(:NEW.ID, 'UPDATE');
  END IF;
END;
/
```  
上面的触发器已经实装。  
2. 收到触发器之后需要获取到异动文件和item 我给你一个如何通过item 获得file_info的路径：  
- 在表Item中会有主键Id
- Item中的主键在Attachment_map 表中对应了Parent_ID的字段，你可以通过此查到这条记录。然后记录下这个记录里的Attach_ID 和 Version值
- 在Version表中有字段Attach_ID 和 Version_Num，这两个字段，分别对应上面的Attachment_map的Attach_ID 和 Version字段的值 ，通过这两个值获取到该表主键Id
- 在VERSION_FILE_MAP表里面有字段VERSION_ID 这个字段就是上面的Version表里面的主键。 通过上面的主键能找到对应的记录，在该条记录中，有字段FILE_ID ， 这个是文件主键。
- 在FILES表中，Id就是刚刚的FILE_ID 的值，通过这个值能搜索到记录，只要获得这个记录里面的FILENAME  
- 在FILE_INFO表中，FILE_ID就是VERSION_FILE_MAP的FILE_ID ,里面IFS_FILEPATH就是相对路径


我把几张表的SQL放到下面  
```SQL
--FILE_INFO
create table FILE_INFO
(
    FILE_ID        NUMBER               not null
        constraint PK_CHECKSUM
            primary key,
    FILE_TYPE      VARCHAR2(450 char),
    CHECKSUM_VALUE NUMBER,
    FILE_PATH      VARCHAR2(4000 char),
    LOCATIONS      VARCHAR2(4000 char),
    EIFS_FILEPATH  VARCHAR2(4000 char),
    IFS_FILEPATH   VARCHAR2(4000 char),
    HFS_FILEPATH   VARCHAR2(4000 char),
    CREATED        DATE default SYSDATE not null,
    LAST_UPD       DATE default SYSDATE not null
)
/
```

```SQL
--FILES
create table FILES
(
    ID                   NUMBER               not null
        constraint FILES_PK
            primary key,
    FILE_SIZE            NUMBER,
    FILE_TYPE            VARCHAR2(450 char),
    FILENAME             VARCHAR2(4000 char),
    PAGE                 NUMBER,
    FLAGS                VARCHAR2(32 char),
    UUID                 VARCHAR2(128 char),
    CONTENT_URL          VARCHAR2(1024 char),
    FILE_FORMAT          VARCHAR2(10 char),
    CONTENT_URL_TEMPLATE VARCHAR2(1024 char),
    CATEGORY             NUMBER,
    CREATED              DATE default SYSDATE not null,
    LAST_UPD             DATE default SYSDATE not null
)
/
```

```SQL
--Version_File_Map
create table VERSION_FILE_MAP
(
    ID                NUMBER                            not null
        constraint VER_FILE_MAP_PK
            primary key,
    VERSION_ID        NUMBER,
    PATH              VARCHAR2(1024 char),
    DESCRIPTION       VARCHAR2(300 char),
    LAST_VIEWED       DATE,
    FLAGS             VARCHAR2(32 char) default '00000000000000000000000000000000',
    DATE01            DATE,
    DATE02            DATE,
    DATE03            DATE,
    DATE04            DATE,
    DATE05            DATE,
    LIST01            NUMBER(10),
    LIST02            NUMBER(10),
    LIST03            NUMBER(10),
    LIST04            NUMBER(10),
    LIST05            NUMBER(10),
    MULTILIST01       VARCHAR2(255 char),
    MULTILIST02       VARCHAR2(255 char),
    MULTILIST03       VARCHAR2(255 char),
    NUMERIC01         NUMBER,
    NUMERIC02         NUMBER,
    NUMERIC03         NUMBER,
    NUMERIC04         NUMBER,
    NUMERIC05         NUMBER,
    TEXT01            VARCHAR2(150 char),
    TEXT02            VARCHAR2(150 char),
    TEXT03            VARCHAR2(150 char),
    TEXT04            VARCHAR2(150 char),
    TEXT05            VARCHAR2(150 char),
    TEXT06            VARCHAR2(150 char),
    TEXT07            VARCHAR2(150 char),
    FILE_ID           NUMBER,
    CHECKOUT_LOCATION VARCHAR2(3000 char),
    MASTER_THUMBNAIL  NUMBER,
    CREATED           DATE              default SYSDATE not null,
    LAST_UPD          DATE              default SYSDATE not null
)
/
```

```SQL
--Version 
create table VERSION
(
    ID                 NUMBER                    not null
        constraint VERSION_PK
            primary key,
    ATTACH_ID          NUMBER,
    VERSION_NUM        NUMBER,
    FLAGS              VARCHAR2(32 char),
    CREATE_DATE        DATE,
    LIFECYCLEPHASE     NUMBER,
    CHECKIN_USER       NUMBER,
    LAST_UPD           DATE      default SYSDATE not null,
    LABEL              VARCHAR2(150 char),
    REVISION           VARCHAR2(150 char),
    VER_DATE           DATE,
    APPROVAL_STATUS    NUMBER(1),
    ITEM_CHANGE_STATUS VARCHAR2(10 char),
    FOLDER_REVISION    VARCHAR2(150),
    CHANGE             NUMBER    default 0,
    INCORPORATED       NUMBER(1) default 0,
    CHANGESEQ          VARCHAR2(20 char),
    DATE16             DATE,
    DATE17             DATE,
    DATE18             DATE,
    DATE19             DATE,
    DATE20             DATE,
    LIST26             NUMBER(10),
    LIST27             NUMBER(10),
    LIST28             NUMBER(10),
    LIST29             NUMBER(10),
    LIST30             NUMBER(10),
    MONEYVALUE11       NUMBER(18, 6),
    MONEYVALUE12       NUMBER(18, 6),
    MONEYVALUE13       NUMBER(18, 6),
    MONEYVALUE14       NUMBER(18, 6),
    MONEYVALUE15       NUMBER(18, 6),
    MONEYCURRENCY11    NUMBER(4),
    MONEYCURRENCY12    NUMBER(4),
    MONEYCURRENCY13    NUMBER(4),
    MONEYCURRENCY14    NUMBER(4),
    MONEYCURRENCY15    NUMBER(4),
    MULTILIST16        VARCHAR2(765 char),
    MULTILIST17        VARCHAR2(765 char),
    MULTILIST18        VARCHAR2(765 char),
    MULTILIST19        VARCHAR2(765 char),
    MULTILIST20        VARCHAR2(765 char),
    NUMERIC11          NUMBER,
    NUMERIC12          NUMBER,
    NUMERIC13          NUMBER,
    NUMERIC14          NUMBER,
    NUMERIC15          NUMBER,
    TEXT26             VARCHAR2(150 char),
    TEXT27             VARCHAR2(150 char),
    TEXT28             VARCHAR2(150 char),
    TEXT29             VARCHAR2(150 char),
    TEXT30             VARCHAR2(150 char)
)
/

```
```SQL
--Item 
create table ITEM
(
    ID                  NUMBER(10)                not null
        constraint ITEM_PK
            primary key,
    CLASS               NUMBER(10),
    SUBCLASS            NUMBER(10),
    ITEM_NUMBER         VARCHAR2(300 char)        not null,
    CATEGORY            NUMBER(10),
    DESCRIPTION         VARCHAR2(720 char),
    DOCSIZE             NUMBER(10),
    MODIFYDATE          DATE,
    OBJVERSION          NUMBER,
    DELETE_FLAG         NUMBER,
    PRODUCT_LINES       VARCHAR2(1024 char),
    FLAGS               VARCHAR2(32 char),
    DEFAULT_CHANGE      NUMBER    default 0,
    COMMODITY           NUMBER,
    ENCODE_NAME         VARCHAR2(300 char)        not null
        constraint ITEM_ENCODE_NAME
            unique,
    PART_FAMILY         NUMBER(10),
    CONV_FACTOR         NUMBER(16, 4),
    IS_TLA              NUMBER(1),
    EXCLUDE_FROM_ROLLUP NUMBER(1) default 0,
    ITEM_GROUP          VARCHAR2(765 char),
    LATEST_RELEASED_ECO NUMBER    default 0       not null,
    MODEL_REF           VARCHAR2(40 char),
    CREATED             DATE      default SYSDATE not null,
    LAST_UPD            DATE      default SYSDATE not null,
    FUNC_TEAM           VARCHAR2(765 char)
)
/
```


```SQL
 -- Attachment 
 create table ATTACHMENT
(
    ID                NUMBER             not null
        constraint ATTACHMENT_PK
            primary key,
    CLASS             NUMBER,
    SUBCLASS          NUMBER,
    ATTACHMENT_NUMBER VARCHAR2(150 char) not null,
    OBJVERSION        NUMBER,
    DELETE_FLAG       NUMBER,
    LATEST_VSN        NUMBER,
    DESCRIPTION       VARCHAR2(4000 char),
    CHECKOUT_USER     NUMBER,
    CHECKOUT_DATE     DATE,
    CHECKOUT_FOLDER   VARCHAR2(3000 char),
    FLAGS             VARCHAR2(32 char),
    LAST_MOD          DATE,
    CREATE_DATE       DATE,
    BASEATTACH_ID     NUMBER,
    ATTACHMENTTYPE    NUMBER,
    COMPONENT_TYPE    NUMBER,
    DEFAULT_CHANGE    NUMBER default 0,
    REDLINE_USER      NUMBER,
    ROUTED_DATE       DATE
)
/
```

```SQL
-- Attachment_map
create table ATTACHMENT_MAP
(
    ID             NUMBER                            not null
        constraint ATTACHMENT_MAP_PK
            primary key,
    PARENT_ID      NUMBER,
    PARENT_ID2     NUMBER,
    ATTACH_ID      NUMBER,
    VERSION        NUMBER,
    PARENT_CLASS   NUMBER,
    DATE01         DATE,
    DATE02         DATE,
    DATE03         DATE,
    DATE04         DATE,
    DATE05         DATE,
    TEXT01         VARCHAR2(150 char),
    TEXT02         VARCHAR2(150 char),
    TEXT03         VARCHAR2(150 char),
    TEXT04         VARCHAR2(150 char),
    TEXT05         VARCHAR2(150 char),
    TEXT06         VARCHAR2(150 char),
    TEXT07         VARCHAR2(150 char),
    TEXT08         VARCHAR2(150 char),
    TEXT09         VARCHAR2(150 char),
    TEXT10         VARCHAR2(150 char),
    TEXT11         VARCHAR2(150 char),
    TEXT12         VARCHAR2(150 char),
    TEXT13         VARCHAR2(150 char),
    TEXT14         VARCHAR2(150 char),
    TEXT15         VARCHAR2(150 char),
    TEXT16         VARCHAR2(150 char),
    TEXT17         VARCHAR2(150 char),
    TEXT18         VARCHAR2(150 char),
    TEXT19         VARCHAR2(150 char),
    TEXT20         VARCHAR2(150 char),
    TEXT21         VARCHAR2(150 char),
    TEXT22         VARCHAR2(150 char),
    TEXT23         VARCHAR2(150 char),
    TEXT24         VARCHAR2(150 char),
    TEXT25         VARCHAR2(150 char),
    LIST01         NUMBER(10),
    LIST02         NUMBER(10),
    LIST03         NUMBER(10),
    LIST04         NUMBER(10),
    LIST05         NUMBER(10),
    LIST06         NUMBER(10),
    LIST07         NUMBER(10),
    LIST08         NUMBER(10),
    LIST09         NUMBER(10),
    LIST10         NUMBER(10),
    LIST11         NUMBER(10),
    LIST12         NUMBER(10),
    LIST13         NUMBER(10),
    LIST14         NUMBER(10),
    LIST15         NUMBER(10),
    LIST16         NUMBER(10),
    LIST17         NUMBER(10),
    LIST18         NUMBER(10),
    LIST19         NUMBER(10),
    LIST20         NUMBER(10),
    LIST21         NUMBER(10),
    LIST22         NUMBER(10),
    LIST23         NUMBER(10),
    LIST24         NUMBER(10),
    LIST25         NUMBER(10),
    MULTILIST01    VARCHAR2(255 char),
    MULTILIST02    VARCHAR2(255 char),
    MULTILIST03    VARCHAR2(255 char),
    NUMERIC01      NUMBER,
    NUMERIC02      NUMBER,
    NUMERIC03      NUMBER,
    NUMERIC04      NUMBER,
    NUMERIC05      NUMBER,
    FLAGS          VARCHAR2(32 char) default '00000000000000000000000000000000',
    VERSION_ID     NUMBER,
    ATTACHMENTTYPE NUMBER,
    FILE_ID        NUMBER,
    LATEST_VSN     NUMBER,
    CREATED        DATE              default SYSDATE not null,
    LAST_UPD       DATE              default SYSDATE not null
)
/
```


3. 构建完成之后把内容发送到FileSyncService子工程的http服务里面，所以你要分析这个子工程。我把这个子工程的http部署到了 10.170.9.4 服务器里面。