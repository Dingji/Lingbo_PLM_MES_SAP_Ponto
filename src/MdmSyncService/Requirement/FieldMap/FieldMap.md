表ITEM 结构如下：
create table ITEM
(
    ID                  NUMBER(10)                not null -- 主键
        constraint ITEM_PK
            primary key,
    CLASS               NUMBER(10), --所属主类
    SUBCLASS            NUMBER(10), -- 所属子类
    ITEM_NUMBER         VARCHAR2(300 char)        not null, -- 物料编号显示值
    CATEGORY            NUMBER(10),  -- 未知
    DESCRIPTION         VARCHAR2(720 char), -- 描述
    DOCSIZE             NUMBER(10), -- 未知
    MODIFYDATE          DATE, --更改时间
    OBJVERSION          NUMBER, -- 版本
    DELETE_FLAG         NUMBER, -- 删除标志
    PRODUCT_LINES       VARCHAR2(1024 char), --产品线，暂时不管
    FLAGS               VARCHAR2(32 char),-- 未知
    DEFAULT_CHANGE      NUMBER    default 0, -- 第一次出现的变更单ID
    COMMODITY           NUMBER, -- 未知
    ENCODE_NAME         VARCHAR2(300 char)        not null -- 物料编号
        constraint ITEM_ENCODE_NAME
            unique,
    PART_FAMILY         NUMBER(10), -- 未知
    CONV_FACTOR         NUMBER(16, 4),-- 未知
    IS_TLA              NUMBER(1),-- 未知
    EXCLUDE_FROM_ROLLUP NUMBER(1) default 0,-- 未知
    ITEM_GROUP          VARCHAR2(765 char),-- 未知
    LATEST_RELEASED_ECO NUMBER    default 0       not null,-- 最后一次变更单ID
    MODEL_REF           VARCHAR2(40 char),-- 未知
    CREATED             DATE      default SYSDATE not null,--创建日期
    LAST_UPD            DATE      default SYSDATE not null, -- 最后更改日期
    FUNC_TEAM           VARCHAR2(765 char) -- 未知
)
/



表NODETABLE 结构如下：
 create table NODETABLE
(
    ID          NUMBER(10)           not null -- 主键
        constraint NODETABLE_PK
            primary key,
    PARENTID    NUMBER(10), -- 他所属的父类NodeTable的id，他就是这个表的Id
    DESCRIPTION VARCHAR2(510 char)   not null, --描述
    OBJTYPE     NUMBER(5), -- 内容类型
    INHERIT     NUMBER(10), -- 未知
    HELPID      NUMBER(10), -- 未知
    VERSION     NUMBER, -- 版本
    NAME        VARCHAR2(255 char), --名称
    CREATED     DATE default SYSDATE not null,--创建时间
    LAST_UPD    DATE default SYSDATE not null -- 最后一次更新时间
)
/


