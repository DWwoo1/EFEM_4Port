<XCom 3.4>
<S3F17 P Customized Proceed with Carrier
	Undefined Structure
>

<S3F18 S Customized Proceed with Carrier Ack
	Undefined Structure
>

<S14F3 P Customized Set Attributes
	Undefined Structure
>

<S14F4 S Customized Set Attributes Ack
	Undefined Structure
>

<S12F3 P Map Setup Data Request
  <LIST 9 
    <MID>
    <IDTYP>
    <MAPFT>
    <FFROT>
    <FNLOC>
    <ORLOC>
    <PRAXI>
    <BCEQU>
    <NULBC>
  >
>

<S12F4 S Map Setup Data
  <LIST 15 
    <MID>
    <IDTYP>
    <FNLOC>
    <ORLOC>
    <RPSEL>
    <LIST 1 
      <REFXY>
    >
    <DUTMS>
    <XDIES>
    <YDIES>
    <ROWCT>
    <COLCT>
    <PRDCT>
    <BCEQU>
    <NULBC>
    <MLCL>
  >
>

<S12F15 P Map Data Request Type 2
  <LIST 2 
    <MID>
    <IDTYP>
  >
>

<S12F16 S Map Data Type 2
  <LIST 4 
    <MID>
    <IDTYP>
    <STRPXY>
    <BINLT>
  >
>

<S12F1 P Map Setup Data Send
  <LIST 15 
    <MID>
    <IDTYP>
    <FNLOC>
    <FFROT>
    <ORLOC>
    <RPSEL>
    <LIST 1 
      <REFXY>
    >
    <DUTMS>
    <XDIES>
    <YDIES>
    <ROWCT>
    <COLCT>
    <NULBC>
    <PRDCT>
    <PRAXI>
  >
>

<S12F2 S Map Setup Data Acknowledge
  <SDACK>
>

<S12F5 P Map Transmit Inquire
  <LIST 4 
    <MID>
    <IDTYP>
    <MAPFT>
    <MLCL>
  >
>

<S12F6 S Map Transmit Grant
  <GRNT1>
>

<S12F9 P Map Data Send Type 2
  <LIST 4 
    <MID>
    <IDTYP>
    <STRPXY>
    <BINLT>
  >
>

<S12F10 S Map Data Ack Type 2
  <MDACK>
>

