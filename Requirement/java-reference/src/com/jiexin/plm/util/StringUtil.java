/*     */ package com.jiexin.plm.util;
/*     */ 
/*     */ import java.math.BigDecimal;
/*     */ import java.text.DateFormat;
/*     */ import java.text.SimpleDateFormat;
/*     */ import java.util.ArrayList;
/*     */ import java.util.Calendar;
/*     */ import java.util.Date;
/*     */ import java.util.List;
/*     */ import java.util.Locale;
/*     */ import java.util.Random;
/*     */ import java.util.TimeZone;
/*     */ import java.util.regex.Matcher;
/*     */ import java.util.regex.Pattern;
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ public class StringUtil
/*     */ {
/*     */   public static boolean isNull(String str) {
/*  24 */     if (str == null || "".equals(str.trim())) {
/*  25 */       return true;
/*     */     }
/*  27 */     return false;
/*     */   }
/*     */   
/*     */   public static String genRandomString() {
/*  31 */     long timeInMillis = Calendar.getInstance().getTimeInMillis();
/*  32 */     return timeInMillis + randomString(5);
/*     */   }
/*     */   
/*     */   public static final String randomString(int length) {
/*  36 */     if (length < 1) {
/*  37 */       return null;
/*     */     }
/*  39 */     Random randGen = new Random();
/*     */     
/*  41 */     char[] numbersAndLetters = "0123456789abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".toCharArray();
/*  42 */     char[] randBuffer = new char[length];
/*  43 */     for (int i = 0; i < randBuffer.length; i++) {
/*  44 */       randBuffer[i] = numbersAndLetters[randGen.nextInt(71)];
/*     */     }
/*     */     
/*  47 */     return new String(randBuffer);
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static Float converToFloat(String str) {
/*  57 */     Float result = null;
/*  58 */     if (!isNull(str) && isNumeric(str)) {
/*  59 */       result = Float.valueOf(Float.parseFloat(str));
/*     */     }
/*  61 */     return result;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static Integer converToInteger(String str) {
/*  71 */     Integer result = null;
/*  72 */     if (!isNull(str) && isNumeric(str)) {
/*  73 */       result = Integer.valueOf(Integer.parseInt(str));
/*     */     }
/*  75 */     return result;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static boolean isNumeric(String str) {
/*  86 */     Pattern pattern = Pattern.compile("([1-9]\\d*\\.?\\d*)|(0\\.\\d*[1-9])");
/*  87 */     Matcher isNum = pattern.matcher(str);
/*  88 */     if (!isNum.matches()) {
/*  89 */       return false;
/*     */     }
/*  91 */     return true;
/*     */   }
/*     */ 
/*     */   
/*     */   public static <E> String converToString(List<E> list) {
/*  96 */     String str = "";
/*  97 */     for (E i : list) {
/*  98 */       str = str + i;
/*     */     }
/* 100 */     return str;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static String theDayAfter() {
/* 110 */     Calendar ca = Calendar.getInstance();
/* 111 */     ca.setTime(new Date());
/* 112 */     ca.add(5, 1);
/* 113 */     Date lastMonth = ca.getTime();
/* 114 */     SimpleDateFormat sdf = new SimpleDateFormat("MM/dd/yyyy");
/* 115 */     String format = sdf.format(lastMonth);
/*     */     
/* 117 */     return format;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static int compare_date(String DATE1, String DATE2) {
/* 129 */     DateFormat df = new SimpleDateFormat("MM/dd/yyyy");
/*     */     try {
/* 131 */       Date dt1 = df.parse(DATE1);
/* 132 */       Date dt2 = df.parse(DATE2);
/* 133 */       if (dt1.getTime() > dt2.getTime())
/* 134 */         return 1; 
/* 135 */       if (dt1.getTime() < dt2.getTime()) {
/* 136 */         return -1;
/*     */       }
/* 138 */       return 0;
/*     */     }
/* 140 */     catch (Exception exception) {
/* 141 */       exception.printStackTrace();
/*     */       
/* 143 */       return 0;
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static boolean isNum(String str) {
/*     */     try {
/* 154 */       new BigDecimal(str);
/* 155 */       return true;
/* 156 */     } catch (Exception e) {
/* 157 */       return false;
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static boolean isInteger(String str) {
/*     */     try {
/* 169 */       new Integer(str);
/* 170 */       return true;
/* 171 */     } catch (Exception e) {
/* 172 */       return false;
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static String changePercentToPoint(String percent) {
/* 185 */     String value = "";
/* 186 */     if (percent != null && !"".equals(percent) && percent.trim().length() > 1) {
/* 187 */       Float number = Float.valueOf((new Float(percent.substring(0, percent.indexOf("%")))).floatValue() / 100.0F);
/* 188 */       value = number.toString();
/*     */     } 
/* 190 */     return value;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static String getSurplusString(String source, String start, String end, String replace) {
/* 202 */     int startIndex = source.indexOf(start);
/* 203 */     if (startIndex != -1) {
/* 204 */       int endIndex = source.indexOf(end, startIndex + 1);
/* 205 */       if (endIndex != -1) {
/* 206 */         String beforeStart = source.substring(0, startIndex + start.length());
/* 207 */         String afterEnd = source.substring(endIndex);
/* 208 */         return beforeStart + replace + afterEnd;
/*     */       } 
/* 210 */       return source;
/*     */     } 
/*     */     
/* 213 */     return source;
/*     */   }
/*     */   
/*     */   public static void main(String[] args) {
/*     */     try {
/* 218 */       List<String> list = new ArrayList<>();
/* 219 */       list.add("1");
/* 220 */       list.add("2");
/* 221 */       list.add("3");
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */     
/*     */     }
/* 229 */     catch (Exception e) {
/* 230 */       e.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static int getStringLength(String str) {
/* 243 */     int length = 0;
/* 244 */     String regex = "[一-龥]";
/* 245 */     for (int i = 0; i < str.length(); i++) {
/*     */       
/* 247 */       String temp = str.substring(i, i + 1);
/* 248 */       if (temp.matches(regex)) {
/* 249 */         length += 3;
/*     */       } else {
/* 251 */         length++;
/*     */       } 
/*     */     } 
/* 254 */     return length;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static int lastLetterIndexFromString(String str) {
/* 264 */     char[] charArray = str.toCharArray();
/* 265 */     int index = 0;
/* 266 */     for (int i = 0; i <= charArray.length - 1; i++) {
/* 267 */       if ((charArray[i] <= 'z' && charArray[i] >= 'a') || (charArray[i] <= 'Z' && charArray[i] >= 'A')) {
/* 268 */         index = i;
/*     */       }
/*     */     } 
/* 271 */     return index;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static boolean isEmpty(String str) {
/* 280 */     if (str != null && !"".equals(str.trim())) {
/* 281 */       return false;
/*     */     }
/* 283 */     return true;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static boolean isNotEmpty(String str) {
/* 292 */     return !isEmpty(str);
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static String dataFormat(String dataTime) {
/* 302 */     Date date = new Date(dataTime);
/* 303 */     SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMddHHmmss");
/* 304 */     String format = sdf.format(date);
/* 305 */     return format;
/*     */   }
/*     */   public static String dataFormats(String dataTime) {
/* 308 */     Date date = new Date(dataTime);
/* 309 */     SimpleDateFormat sdf = new SimpleDateFormat("yyyy/MM/dd");
/* 310 */     String format = sdf.format(date);
/* 311 */     return format;
/*     */   }
/*     */   
/*     */   public static String dataCrateFormat(String dataTime) {
/* 315 */     Date date = new Date(dataTime);
/* 316 */     SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMddHHmmss");
/* 317 */     String format = sdf.format(date);
/* 318 */     String substring = format.substring(0, 8);
/* 319 */     return substring;
/*     */   }
/*     */   
/*     */   public static String TimeCrateFormat(String dataTime) {
/* 323 */     Date date = new Date(dataTime);
/* 324 */     SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMddHHmmss");
/* 325 */     String format = sdf.format(date);
/* 326 */     String substring = format.substring(8);
/* 327 */     return substring;
/*     */   }
/*     */   public static Date FormatTR1(String dataTime) throws Exception {
/* 330 */     DateFormat cst = new SimpleDateFormat("yyyy/MM/dd HH:mm:ss");
/* 331 */     DateFormat gmt = new SimpleDateFormat("EEE MMM dd HH:mm:ss zzz yyyy", Locale.ENGLISH);
/* 332 */     cst.setTimeZone(TimeZone.getTimeZone("ETC/GMT"));
/* 333 */     gmt.setTimeZone(TimeZone.getTimeZone("ETC/GMT"));
/* 334 */     Date dateTime = gmt.parse(dataTime);
/* 335 */     String dateString = cst.format(dateTime);
/* 336 */     Date parse = new Date(dateString);
/* 337 */     return parse;
/*     */   }
/*     */   public static String FormatTR(Date dataTime) throws Exception {
/* 340 */     DateFormat cst = new SimpleDateFormat("yyyyMMdd");
/* 341 */     cst.setTimeZone(TimeZone.getTimeZone("ETC/GMT-8"));
/* 342 */     String dateString = cst.format(dataTime);
/* 343 */     return dateString;
/*     */   }
/*     */   public static String FormatTRs(String dataTime) throws Exception {
/* 346 */     DateFormat cst = new SimpleDateFormat("yyyy/MM/dd HH:mm:ss");
/* 347 */     DateFormat gmt = new SimpleDateFormat("EEE MMM dd HH:mm:ss zzz yyyy", Locale.ENGLISH);
/* 348 */     cst.setTimeZone(TimeZone.getTimeZone("ETC/GMT-8"));
/* 349 */     gmt.setTimeZone(TimeZone.getTimeZone("ETC/GMT-8"));
/* 350 */     Date dateTime = gmt.parse(dataTime);
/* 351 */     String dateString = cst.format(dateTime);
/* 352 */     Date parse = new Date(dateString);
/* 353 */     Calendar cal = Calendar.getInstance();
/* 354 */     cal.setTime(parse);
/* 355 */     cal.add(10, -8);
/* 356 */     Date times = cal.getTime();
/* 357 */     String time = cst.format(times);
/* 358 */     return time;
/*     */   }
/*     */   public static Date Formats(String dataTime) throws Exception {
/* 361 */     SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
/* 362 */     Date parse = sdf.parse(dataTime);
/* 363 */     return parse;
/*     */   }
/*     */   
/*     */   public static String FormatTime(Date dataTime) throws Exception {
/* 367 */     TimeZone tz = TimeZone.getTimeZone("UTC");
/* 368 */     DateFormat dft = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'");
/* 369 */     dft.setTimeZone(tz);
/* 370 */     String format = dft.format(dataTime);
/* 371 */     return format;
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\StringUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */