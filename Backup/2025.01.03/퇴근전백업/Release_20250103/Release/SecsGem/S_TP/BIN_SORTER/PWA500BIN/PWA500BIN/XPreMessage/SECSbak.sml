<XCom 3.4>
<S1F0 N Abort Transaction (S1F0)
>

<S1F1 P Are You There Request (R)
>

<S1F2 S On Line Data (D)
  <LIST 2 
    <MDLN>
    <SOFTREV>
  >
>

<S1F2 S On Line Data (D)
  <LIST 0 
  >
>

<S1F3 P Selected Equipment Status Request (SSR)
  <LIST n 
    <SVID>
  >
>

<S1F4 S Selected Equipment Status Data (SSD)
	Undefined Structure
>
<S1F11 P Status Variable Namelist Request (SVNR)
  <LIST n 
    <SVID>
  >
>

<S1F12 S Status Variable Namelist Reply (SVNRR)
  <LIST n 
    <LIST 3 
      <SVID>
      <SVNAME>
      <UNITS>
    >
  >
>

<S1F13 P Establish Communications Request (CR)
  <LIST 2 
    <MDLN>
    <SOFTREV>
  >
>

<S1F13 P Establish Communications Request (CR)
  <LIST 0 
  >
>

<S1F14 S Establish Communications Request Acknowledge (CRA)
  <LIST 2 
    <COMMACK>
    <LIST 2 
      <MDLN>
      <SOFTREV>
    >
  >
>

<S1F14 S Establish Communications Request Acknowledge (CRA)
  <LIST 2 
    <COMMACK>
    <LIST 0 
    >
  >
>

<S1F15 P Request OFF-LINE (ROFL)
>

<S1F16 S OFF-LINE Acknowledge (OFLA)
  <OFLACK>
>

<S1F17 P Request ON-LINE (RONL)
>

<S1F18 S ON-LINE Acknowledge (ONLA)
  <ONLACK>
>

<S1F21 P Data Variable Namelist Request (DVNR)
  <LIST n 
    <VID>
  >
>

<S1F22 S Data Variable Namelist (DVN)
  <LIST n 
    <LIST 3 
      <VID>
      <DVVALNAME>
      <UNITS>
    >
  >
>

<S1F23 P Collection Event Namelist (CEN)
  <LIST n 
    <CEID>
  >
>

<S1F24 S Collection Event Namelist (CEN)
  <LIST n 
    <LIST 3 
      <CEID>
      <CENAME>
      <LIST n 
        <VID>
      >
    >
  >
>

<S2F0 N Abort Transaction (S2F0)
>

<S2F13 P Equipment Constant Request (ECR)
  <LIST n 
    <ECID>
  >
>

<S2F14 S Equipment Constant Data (ECD)
	Undefined Structure
>
<S2F15 P New Equipment Constant Send (ECS)
	Undefined Structure
>
<S2F16 S New Equipment Constant Acknowledge (ECA)
  <EAC>
>

<S2F17 P Date and Time Request (DTR)
>

<S2F18 S Date and Time Data (DTD)
  <TIME>
>

<S2F23 P Trace Initialize Send (TIS)
  <LIST 5 
    <TRID>
    <DSPER>
    <TOTSMP>
    <REPGSZ>
    <LIST n 
      <SVID>
    >
  >
>

<S2F24 S Trace Initialize Acknowledge (TIA)
  <TIAACK>
>

<S2F25 P Loopback Diagnostic Request (LDR)
  <ABS>
>

<S2F26 S Loopback Diagnostic Data (LDD)
  <ABS>
>

<S2F29 P Equipment Constant Namelist Request (ECNR)
  <LIST n ECID Lists
    <ECID>
  >
>

<S2F30 S Equipment Constant Namelist (ECN)
	Undefined Structure
>
<S2F31 P Data and Time Set Request (DTS)
  <TIME>
>

<S2F32 S Date and Time Set Acknowledge (DTA)
  <TIACK>
>

<S2F33 P Define Report (DR)
  <LIST 2 
    <DATAID>
    <LIST n 
      <LIST 2 
        <RPTID>
        <LIST n 
          <VID>
        >
      >
    >
  >
>

<S2F34 S Define Report Acknowledge (DRA)
  <DRACK>
>

<S2F35 P Link Event Report (LER)
  <LIST 2 
    <DATAID>
    <LIST n 
      <LIST 2 
        <CEID>
        <LIST n 
          <RPTID>
        >
      >
    >
  >
>

<S2F36 S Link Event Report Acknowledge (LERA)
  <LRACK>
>

<S2F37 P Enable/Disable Event Report (EDER)
  <LIST 2 
    <CEED>
    <LIST n 
      <CEID>
    >
  >
>

<S2F38 S Enable/Disable Event Report Acknowledge (EERA)
  <ERACK>
>

<S2F39 P Multi Block Inquire (DMBI)
  <LIST 2 
    <DATAID>
    <DATALENGTH>
  >
>

<S2F40 S Multi Block Grant (DMBG)
  <GRANT>
>

<S2F41 P Host Command Send (HCS)
  <LIST 2 
    <RCMD>
    <LIST n 
      <LIST 2 
        <CPNAME>
        <CPVAL>
      >
    >
  >
>

<S2F42 S Host Command Acknowledge (HCA)
  <LIST 2 
    <HCACK>
    <LIST n 
      <LIST 2 
        <CPNAME>
        <CPACK>
      >
    >
  >
>

<S2F43 P Reset Spooling Streams and Functions (RSSF)
  <LIST n 
    <LIST 2 
      <STRID>
      <LIST n 
        <FCNID>
      >
    >
  >
>

<S2F44 S Reset Spooling Acknowledge (RSA)
  <LIST 2 
    <RSPACK>
    <LIST n 
      <LIST 3 
        <STRID>
        <STRACK>
        <LIST n 
          <FCNID>
        >
      >
    >
  >
>

<S2F45 P Define Variable Limit Attributes (DVLA)
  <LIST 2 
    <DATAID>
    <LIST n 
      <LIST 2 
        <VID>
        <LIST n 
          <LIST 2 
            <LIMITID>
            <LIST 2 
              <UPPERDB>
              <LOWERDB>
            >
          >
        >
      >
    >
  >
>

<S2F46 S Variable Limit Attribute Acknowledge (VLAA)
  <LIST 2 
    <VLAACK>
    <LIST n 
      <LIST 3 
        <VID>
        <LVACK>
        <LIST 0 
        >
      >
    >
  >
>

<S2F46 S Variable Limit Attribute Acknowledge (VLAA)
  <LIST 2 
    <VLAACK>
    <LIST n 
      <LIST 3 
        <VID>
        <LVACK>
        <LIST 2 
          <LIMITID>
          <LIMITACK>
        >
      >
    >
  >
>

<S2F47 P Variable Limit Attribute Request (VLAR)
  <LIST n 
    <VID>
  >
>

<S2F48 S Variable Limit Attributes Send (VLAS)
  <LIST n 
    <LIST 2 
      <VID>
      <LIST 0 
      >
    >
  >
>

<S2F48 S Variable Limit Attributes Send (VLAS)
  <LIST n 
    <LIST 2 
      <VID>
      <LIST 4 
        <UNITS>
        <LIMITMIN>
        <LIMITMAX>
        <LIST n 
          <LIST 3 
            <LIMITID>
            <UPPERDB>
            <LOWERDB>
          >
        >
      >
    >
  >
>

<S3F0 N Abort Transaction
>

<S3F17 P Carrier Action Request
	Undefined Structure
>

<S3F18 S Carrier Action Acknowledge
	Undefined Structure
>

<S3F19 P Cancel All Carrier Out Request
>

<S3F20 S Cancel All Carrier Out Acknowledge
  <LIST 2 
    <CAACK>
    <LIST n 
      <LIST 2 
        <ERRCODE>
        <ERRTEXT>
      >
    >
  >
>

<S3F25 P Port Action Request
  <LIST 3 
    <PORTACTION>
    <PTN>
    <LIST n 
      <LIST 2 
        <CATTRID>
        <CATTRDATA>
      >
    >
  >
>

<S3F26 S Port Action Acknowledge
  <LIST 2 
    <CAACK>
    <LIST n 
      <LIST 2 
        <ERRCODE>
        <ERRTEXT>
      >
    >
  >
>

<S3F27 P Port AMHS Request
  <LIST 2 
    <ACCESSMODE>
    <LIST n 
      <PTN>
    >
  >
>

<S3F28 S Port AMHS Acknowledge
  <LIST 2 
    <CAACK>
    <LIST n 
      <LIST 3 
        <PTN>
        <ERRCODE>
        <ERRTEXT>
      >
    >
  >
>

<S5F0 N Abort Transaction (S5F0)
>

<S5F1 P Alarm Report Send (ARS)
  <LIST 3 
    <ALCD>
    <ALID>
    <ALTX>
  >
>

<S5F2 S Alarm Report Acknowledge (ARA)
  <ACKC5>
>

<S5F3 P Enable/Disable Alarm Send (EAS)
  <LIST 2 
    <ALED>
    <ALID>
  >
>

<S5F4 S Enable/Disable Alarm Acknowledge (EAA)
  <ACKC5>
>

<S5F5 P List Alarms Request (LAR)
  <LIST n 
    <ALID>
  >
>

<S5F5 P List Alarms Request (LAR)
  <ALID>
>

<S5F6 S List Alarms Data (LAD)
  <LIST n 
    <LIST 3 
      <ALCD>
      <ALID>
      <ALTX>
    >
  >
>

<S5F7 P List Alarms Enable Request (LEAR)
>

<S5F8 S List Alarms Enable Data (LEAD)
  <LIST n 
    <LIST 3 
      <ALCD>
      <ALID>
      <ALTX>
    >
  >
>

<S6F0 N Abort Transaction (S6F0)
>

<S6F1 P Trace Data Send (TDS)
	Undefined Structure
>
<S6F2 S Trace Data Acknowledge (TDA)
  <ACKC6>
>

<S6F11 P Event Report Send (ERS)
	Undefined Structure
>
<S6F12 S Event Report Acknowledge (ERA)
  <ACKC6>
>

<S6F15 P Event Report Request (ERR)
  <CEID>
>

<S6F16 S Event Report Data (ERD)
	Undefined Structure
>
<S6F19 P Individual Report Request (IRR)
  <RPTID>
>

<S6F20 S Individual Report Data (IRD)
	Undefined Structure
>
<S6F23 P Request Spooled Data (RSD)
  <RSDC>
>

<S6F24 S Request Spooled Data Acknowledgement Send (RSDAS)
  <RSDA>
>

<S7F0 N Abort Transaction (S7F0)
>

<S7F1 P Process Program Load Inquire (PPI)
  <LIST 2 
    <PPID>
    <LENGTH>
  >
>

<S7F2 S Process Program Load Grant (PPG)
  <PPGNT>
>

<S7F3 P Process Program Send (PPS)
  <LIST 2 
    <PPID>
    <PPBODY>
  >
>

<S7F4 S Process Program Acknowledge (PPA)
  <ACKC7>
>

<S7F5 P Process Program Request (PPR)
  <PPID>
>

<S7F6 S Process Program Data (PPD)
  <LIST 2 
    <PPID>
    <PPBODY>
  >
>

<S7F6 S Process Program Data (PPD)
  <LIST 0 
  >
>

<S7F17 P Delete Process Program Send (DPS)
  <LIST n 
    <PPID>
  >
>

<S7F18 S Delete Process Program Acknowledge (DPA)
  <ACKC7>
>

<S7F19 P Current EPPD Request (RER)
>

<S7F20 S Current EPPD Data (RED)
  <LIST n 
    <PPID>
  >
>

<S7F23 P Formatted Process Program Send (FPS)
  <LIST 4 
    <PPID>
    <MDLN>
    <SOFTREV>
    <LIST n CCODE n PPARM Lists
      <LIST 2 n-th list items
        <CCODE>
        <LIST n PPARM Lists
          <PPARM>
        >
      >
    >
  >
>

<S7F23 P Formatted Process Program Send (FPS)
  <LIST 4 
    <PPID>
    <MDLN>
    <SOFTREV>
    <LIST n CCODE n PPARM Lists
      <LIST 2 n-th list items
        <CCODE>
        <LIST n PPARM Lists
          <LIST 2 
            <PPNAME>
            <PPVALUE>
          >
        >
      >
    >
  >
>

<S7F24 S Formatted Process Program Acknowledge (FPA)
  <ACKC7>
>

<S7F25 P Formatted Process Program Request (FPR)
  <PPID>
>

<S7F26 S Formatted Process Program Data (FPD)
  <LIST 4 
    <PPID>
    <MDLN>
    <SOFTREV>
    <LIST n Number of Process Commands
      <LIST 2 
        <CCODE>
        <LIST n Number of Parameters
          <PPARM>
        >
      >
    >
  >
>

<S7F26 S Extension Formatted Process Program Data (FPD)
  <LIST 4 
    <PPID>
    <MDLN>
    <SOFTREV>
    <LIST n Number of Process Commands
      <LIST 2 
        <CCODE>
        <LIST n PPARM Lists
          <LIST 2 
            <PPNAME>
            <PPVALUE>
          >
        >
      >
    >
  >
>

<S7F26 S Formatted Process Program Data (FPD)
  <LIST 0 
  >
>

<S7F27 P Process Program Verification Send (PVS)
  <LIST 2 
    <PPID>
    <LIST n number of errors being reported
      <LIST 3 
        <ACKC7A>
        <SEQNUM>
        <ERRW7>
      >
    >
  >
>

<S7F28 S Process Program Verification Acknowledge (PVA)
>

<S10F0 N Abort Transaction (S10F0)
>

<S10F1 P Terminal Request (TRN)
  <LIST 2 
    <TID>
    <TEXT>
  >
>

<S10F2 S Terminal Request Acknowledge (TRA)
  <ACKC10>
>

<S10F3 P Terminal Display, Single (VTN)
  <LIST 2 
    <TID>
    <TEXT>
  >
>

<S10F4 S Terminal Display, Single Acknowledge (VTA)
  <ACKC10>
>

<S10F5 P Terminal Display, Multi-Block (VTN)
  <LIST 2 
    <TID>
    <LIST n 
      <TEXT>
    >
  >
>

<S10F6 S Terminal Display, Multi-Block Acknowledge (VMA)
  <ACKC10>
>

<S14F0 N Abort Transaction (S14F0)
>

<S14F1 P GetAttr Request (GAR)
	Undefined Structure
>

<S14F2 S GetAttr Data (GAD)
	Undefined Structure
>

<S14F3 P SetAttr Request (SAR)
	Undefined Structure
>

<S14F4 S SetAttr Data (SAD)
	Undefined Structure
>

<S14F9 P Create Object Request (COR)
	Undefined Structure
>

<S14F10 S Create Object Acknowledge (COA)
	Undefined Structure
>

<S14F11 P Delete Object Request (DOR)
	Undefined Structure
>

<S14F12 S Delete Object Acknowledge (COA)
	Undefined Structure
>

<S14F19 P Generic Service Request (GSR)
  <LIST 5 
    <DATAID>
    <OPID>
    <OBJSPEC>
    <SVCNAME>
    <LIST n 
      <LIST 2 
        <SPNAME>
        <SPVAL>
      >
    >
  >
>

<S14F20 S Generic Service Acknowledge (GSA)
	Undefined Structure
>

<S16F0 N Abort Transaction (S16F0)
>

<S16F1 P Multi-block Process Job Data Inquire (PRJI)
  <LIST 2 
    <DATAID>
    <DATALENGTH>
  >
>

<S16F2 S Multi-block Process Job Data Grant (PRJG)
  <GRANT>
>

<S16F5 P Process Job Command Request (PRJCMDR)
  <LIST 4 
    <DATAID>
    <PRJOBID>
    <PRCMDNAME>
    <LIST 0 
    >
  >
>

<S16F6 S Process Job Command Acknowledge (PRJCMDA)
	Undefined Structure
>

<S16F11 P Enhanced Process Job Create
	Undefined Structure
>

<S16F12 S Process Job Create Enhanced Acknowledge
  <LIST 2 
    <PRJOBID>
    <LIST 2 
      <ACKA>
      <LIST n 
        <LIST 2 
          <ERRCODE>
          <ERRTEXT>
        >
      >
    >
  >
>

<S16F15 P Multi-Process Job Create
	Undefined Structure
>

<S16F16 S Multi-Process Job Acknowledge
	Undefined Structure
>

<S16F17 P PRJobDequeue
  <LIST n 
    <PRJOBID>
  >
>

<S16F18 S PRJobDequeue Acknowledge
	Undefined Structure
>

<S16F19 P PRGetAllJobs Request
>

<S16F20 S PRGetAllJobs Reply
  <LIST n 
    <LIST 2 
      <PRJOBID>
      <PRSTATE>
    >
  >
>

<S16F21 P PRGetSpace Request
>

<S16F22 S PRGetSpace Reply
  <PRJOBSPACE>
>

<S16F23 P PRJobSetRecipeVariable
  <LIST 2 
    <PRJOBID>
    <LIST n 
      <LIST 2 
        <RCPPARNM>
        <RCPPARVAL>
      >
    >
  >
>

<S16F24 S PRJobSetRecipeVariable Acknowledge
	Undefined Structure
>

<S16F25 P PRJobSetStartMethod
  <LIST 2 
    <LIST n 
      <PRJOBID>
    >
    <PRPROCESSSTART>
  >
>

<S16F26 S PRJobSetStartMethod Acknowledge
	Undefined Structure
>

<S16F27 P Control Job Command Request
  <LIST 3 
    <CTLJOBID>
    <CTLJOBCMD>
    <LIST 2 
      <CPNAME>
      <CPVAL>
    >
  >
>

<S16F27 P Control Job Command Request
  <LIST 3 
    <CTLJOBID>
    <CTLJOBCMD>
    <LIST 0 
    >
  >
>

<S16F28 S Control Job Command Acknowledge
  <LIST 2 
    <ACKA>
    <LIST 2 
      <ERRCODE>
      <ERRTEXT>
    >
  >
>

<S16F28 S Control Job Command Acknowledge
  <LIST 2 
    <ACKA>
    <LIST 0 
    >
  >
>

<S16F29 P PRSetMtrlOrder (PRJSMO)
  <PRMTRLORDER>
>

<S16F30 S PRSetMtrlOrder Acknowledge (PRJSMOA)
  <ACKA>
>

<S21F1 P Item Load Inquire
  <LIST 4 
    <ITEMTYPE>
    <ITEMID>
    <ITEMLENGTH>
    <ITEMVERSION>
  >
>

<S21F2 S Item Load Grant
  <LIST 2 
    <ITEMACK>
    <ITEMERROR>
  >
>

<S21F3 P Item Send
  <LIST 5 
    <ITEMTYPE>
    <ITEMID>
    <ITEMLENGTH>
    <ITEMVERSION>
    <LIST n 
      <ITEMPART>
    >
  >
>

<S21F4 S Item Send Acknowledge
  <LIST 2 
    <ITEMACK>
    <ITEMERROR>
  >
>

<S21F5 P Item Request
  <LIST 2 
    <ITEMTYPE>
    <ITEMID>
  >
>

<S21F6 S Item Data
  <LIST 7 
    <ITEMACK>
    <ITEMERROR>
    <ITEMTYPE>
    <ITEMID>
    <ITEMLENGTH>
    <ITEMVERSION>
    <LIST n 
      <ITEMPART>
    >
  >
>

<S21F7 P Item Type List Request
  <ITEMTYPE>
>

<S21F8 S Item Type List Results
  <LIST 4 
    <ITEMACK>
    <ITEMERROR>
    <ITEMTYPE>
    <LIST n 
      <LIST 3 
        <ITEMID>
        <ITEMLENGTH>
        <ITEMVERSION>
      >
    >
  >
>

<S21F9 P Supported Item Type List Request
>

<S21F10 S Supported Item Type List Results
  <LIST 3 
    <ITEMACK>
    <ITEMERROR>
    <LIST n 
      <ITEMTYPE>
    >
  >
>

<S21F11 P Item Delete
  <LIST 2 
    <ITEMTYPE>
    <LIST n 
      <ITEMID>
    >
  >
>

<S21F12 S Item Delete Acknowledge
  <LIST 3 
    <ITEMACK>
    <ITEMTYPE>
    <LIST n 
      <LIST 3 
        <ITEMID>
        <ITEMACK>
        <ITEMERROR>
      >
    >
  >
>

<S21F19 P Item Type Feature Support
  <LIST n 
    <ITEMTYPE>
  >
>

<S21F20 S Item Type Feature Support Results
  <LIST n 
    <LIST 4 
      <ITEMACK>
      <ITEMERROR>
      <ITEMTYPE>
      <ITEMTYPESUPPORT>
    >
  >
>

