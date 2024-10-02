# Zatca eInvoice SDK Library In Action

This is sample C# Code how to use .Net 8  and NET Framework 4.8 Zatca eInvoice SDK library (*zatca-einvoicing-sdk-238-R3.3.5*) provided by Zatca.

This code is full step to get Integrating with Zatca

1. Onboarding
   - Generate CSR and PrivateKey
   - Get Compliance CSID
   - Sending Sample Invoice to Compliance Check Url (Standar, Credit Note and Debit Note for Standard and Simplified)
   - Get Production CSID
   - Save OnboardingInfo to File as Reference for Approval Process
      
2. Invoice Approval (Clearance & Report) 
   - Standard Invoice
   - Standard Credit Note
   - Standard Debit Note
   - Simplified Invoice
   - Simplified Credit Note
   - Simplified Debit Note

All Done In Sandbox Protal (Non Production Environtment) and Should not any Problem for Simulation and Production Environment

It just Simple code, that show how working with Zatca SDK.  Ofcourse, we can use another feature that provided in Zatca SDK

Please make sure we copy ikvm folder from Test folder in Zatca eInvoice SDK to \bin\Debug\net8.0 if we want to use Validation Method from Zatca.eInvoice.SDK.

**eInvoice Validation is working, but not sure if this OK.**
**Please Let me know the result, if anyone can test it in Simulation Envoronment.**

```

Starting the console app...

Starting Onboarding process...

Step 1: Generating CSR and PrivateKey
CSR and PrivateKey generated successfully

Step 2: Getting Compliance CSID
CCSID obtained successfully

Step 3: Sending Sample Documents

Processing Standard Invoice...
Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Standard Invoice processed successfully


Processing Standard CreditNote...
Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Standard CreditNote processed successfully


Processing Standard DebitNote...
Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Standard DebitNote processed successfully


Processing Simplified Invoice...
Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Simplified Invoice processed successfully


Processing Simplified CreditNote...
Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Simplified CreditNote processed successfully


Processing Simplified DebitNote...
Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!

Simplified DebitNote processed successfully



Step 4: Getting Production CSID
PCSID obtained successfully


Onboarding process completed successfully.


Starting Test Approval...


1. Get Standard Invoice Approval

Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Clearance Standard Credit Note

{
  "requestType": "Clearance",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single",
  "statusCode": "200-OK",
  "clearanceStatus": "CLEARED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  },
  "clearedInvoice": ".....="
}


Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Clearance Standard Credit Note

{
  "requestType": "Clearance",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single",
  "statusCode": "200-OK",
  "clearanceStatus": "CLEARED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  },
  "clearedInvoice": "......+"
}


Initialization Step (Standard EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Clearance Standard Debit Note

{
  "requestType": "Clearance",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/clearance/single",
  "statusCode": "200-OK",
  "clearanceStatus": "CLEARED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  },
  "clearedInvoice": "......="
}



2. Get Simplified Invoice Approval

Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Reporting Simplified Invoice

{
  "requestType": "Reporting",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single",
  "statusCode": "200-OK",
  "reportingStatus": "REPORTED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  }
}


Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Reporting Simplified Credit Note

{
  "requestType": "Reporting",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single",
  "statusCode": "200-OK",
  "reportingStatus": "REPORTED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  }
}


Initialization Step (Simplified EInvoice) : True
Validate XSD : True
Validate EN Schematrons : True
Validate KSA Schematrons : True
Generate EInvoice Hash : True
Parse Certificate : True
Generate EInvoice QR : True
Validate QR Code : True
Validate EInvoice Signature : True
Validate EInvoice PIH : True

Overall Signed Invoice Validation : True!
Reporting Simplified Debit Note

{
  "requestType": "Reporting",
  "requestUrl": "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/invoices/reporting/single",
  "statusCode": "200-OK",
  "reportingStatus": "REPORTED",
  "validationResults": {
    "status": "PASS",
    "infoMessages": [
      {
        "status": "PASS",
        "type": "INFO",
        "code": "XSD_ZATCA_VALID",
        "category": "XSD validation",
        "message": "Complied with UBL 2.1 standards in line with ZATCA specifications"
      }
    ],
    "warningMessages": [],
    "errorMessages": []
  }
}




ALL DONE!



C:\Users\Incredible One\source\repos\ZatcaWithSDK\ZatcaWithSDK\bin\Debug\net8.0\ZatcaWithSDK.exe (process 5784) exited with code 0.
To automatically close the console when debugging stops, enable Tools->Options->Debugging->Automatically close the console when debugging stops.
Press any key to close this window . . .


```

Reference :
- https://zatca.gov.sa/en/E-Invoicing/Pages/default.aspx
- https://sandbox.zatca.gov.sa/downloadSDK
- https://sandbox.zatca.gov.sa/IntegrationSandbox
- https://zatca1.discourse.group/

Thank you.
