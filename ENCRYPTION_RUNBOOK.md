# Encryption Operations Runbook

## Purpose

This runbook provides step-by-step procedures for managing encryption keys, responding to security incidents, and maintaining the encryption infrastructure for MercatoApp.

## Key Management

### Routine Key Rotation (Every 90 Days)

**Prerequisites**:
- Access to production key management system (Azure Key Vault, AWS KMS, etc.)
- Administrative access to production environment
- Scheduled maintenance window (recommended, but not required)

**Procedure**:

1. **Pre-rotation Verification**
   ```bash
   # Verify current key version
   az keyvault secret show --vault-name <vault-name> --name MercatoApp-Encryption-CurrentKeyVersion
   
   # Check last rotation date
   az keyvault secret show --vault-name <vault-name> --name MercatoApp-Encryption-LastKeyRotation
   ```

2. **Generate New Key**
   ```bash
   # Generate new version in Key Vault
   NEW_VERSION=$((CURRENT_VERSION + 1))
   
   # Generate secure random key (32 bytes for AES-256)
   openssl rand -base64 32 > new_key.txt
   
   # Upload to Key Vault
   az keyvault secret set \
     --vault-name <vault-name> \
     --name MercatoApp-MasterKey-v${NEW_VERSION} \
     --value @new_key.txt
   
   # Securely delete local key file
   shred -u new_key.txt
   ```

3. **Update Configuration**
   ```bash
   # Update current key version
   az keyvault secret set \
     --vault-name <vault-name> \
     --name MercatoApp-Encryption-CurrentKeyVersion \
     --value ${NEW_VERSION}
   
   # Update last rotation timestamp
   az keyvault secret set \
     --vault-name <vault-name> \
     --name MercatoApp-Encryption-LastKeyRotation \
     --value $(date -u +"%Y-%m-%dT%H:%M:%SZ")
   ```

4. **Restart Application**
   ```bash
   # Azure App Service
   az webapp restart --name <app-name> --resource-group <resource-group>
   
   # Kubernetes
   kubectl rollout restart deployment/mercato-app -n <namespace>
   ```

5. **Verify Rotation**
   ```bash
   # Check application logs for key initialization
   az webapp log tail --name <app-name> --resource-group <resource-group>
   
   # Look for: "Initialized key management service with X key versions"
   ```

6. **Monitor Application**
   - Check application health endpoints
   - Monitor error rates for 30 minutes
   - Verify new encryptions use new key version
   - Confirm existing data can still be decrypted

7. **Document Rotation**
   - Update key rotation log
   - Record new key version
   - Note any issues encountered

**Rollback Procedure** (if issues occur):

```bash
# Revert to previous key version
az keyvault secret set \
  --vault-name <vault-name> \
  --name MercatoApp-Encryption-CurrentKeyVersion \
  --value ${PREVIOUS_VERSION}

# Restart application
az webapp restart --name <app-name> --resource-group <resource-group>
```

### Emergency Key Rotation (Security Incident)

**When to Execute**:
- Suspected key compromise
- Security breach involving encrypted data
- Unauthorized access to key storage
- Compliance requirement

**Procedure**:

1. **Immediate Response**
   ```bash
   # Rotate immediately without waiting
   # Follow steps 2-6 from Routine Key Rotation
   # Do NOT schedule maintenance window - execute immediately
   ```

2. **Generate Incident Ticket**
   - Document reason for emergency rotation
   - Record time of suspected compromise
   - List affected systems

3. **Audit Access Logs**
   ```bash
   # Azure Key Vault
   az monitor activity-log list \
     --resource-group <resource-group> \
     --start-time <incident-start-time> \
     --offset 7d
   
   # AWS CloudTrail
   aws cloudtrail lookup-events \
     --lookup-attributes AttributeKey=ResourceType,AttributeValue=AWS::KMS::Key \
     --start-time <incident-start-time>
   ```

4. **Identify Affected Data**
   - Determine which data was encrypted with compromised key
   - Generate list of affected records
   - Assess potential exposure

5. **Re-encrypt Affected Data**
   ```csharp
   // Execute re-encryption job
   // This should be implemented as a background job
   await dataReEncryptionService.ReEncryptWithNewKeyAsync(
       oldKeyVersion: compromisedVersion,
       newKeyVersion: currentVersion);
   ```

6. **Revoke Compromised Keys**
   ```bash
   # Mark old key as disabled/revoked
   az keyvault secret set \
     --vault-name <vault-name> \
     --name MercatoApp-MasterKey-v${COMPROMISED_VERSION} \
     --enabled false
   ```

7. **Notify Stakeholders**
   - Security team
   - Compliance officer
   - Data protection officer
   - Management (as required)

8. **Post-Incident Review**
   - Document root cause
   - Implement preventive measures
   - Update incident response procedures

## Monitoring and Alerts

### Key Rotation Monitoring

**Alert Conditions**:
- Key rotation overdue (>95 days since last rotation)
- Key rotation failure
- Multiple failed decryption attempts

**Response**:
1. Investigate cause of overdue rotation
2. Schedule immediate rotation if needed
3. Review automation configuration

### Encryption Failures

**Alert Conditions**:
- Encryption operation failures >0.1%
- Decryption operation failures >0.1%
- Authentication tag validation failures (tampering indicator)

**Response**:
1. Check application logs for error details
2. Verify key availability
3. Assess if incident response is needed
4. Create support ticket if issue persists

### Certificate Expiration

**Alert Thresholds**:
- 30 days: Warning
- 14 days: High priority
- 7 days: Critical

**Response**:
```bash
# Renew certificate (Let's Encrypt example)
certbot renew --cert-name <domain>

# Azure App Service (automatic renewal)
# Verify auto-renewal is enabled
az webapp config ssl list --resource-group <resource-group>

# AWS Certificate Manager (automatic renewal)
# Verify certificate is eligible for automatic renewal
aws acm describe-certificate --certificate-arn <arn>
```

## Backup and Recovery

### Backup Encryption Keys

**Frequency**: After each key rotation

**Procedure**:
```bash
# Export keys to encrypted backup (offline storage)
az keyvault backup -n <vault-name> --file keyvault-backup.bin

# Store in secure offline location (HSM, safe, etc.)
# Never store backups in the same location as primary keys
```

**Recovery**:
```bash
# Restore from backup (disaster recovery only)
az keyvault restore -n <vault-name> --file keyvault-backup.bin
```

### Database Backup Encryption

**Verify backup encryption**:
```sql
-- SQL Server
SELECT 
    database_name,
    encryption_state,
    encryption_state_desc,
    key_algorithm,
    key_length
FROM sys.dm_database_encryption_keys;

-- Ensure encryption_state = 3 (encrypted)
```

## Compliance Verification

### Monthly Security Checklist

- [ ] Review encryption logs for anomalies
- [ ] Verify no encryption keys in application logs
- [ ] Check TLS/SSL certificate expiration dates
- [ ] Confirm HTTPS enforcement is active
- [ ] Review failed decryption attempts
- [ ] Verify backup encryption is functioning

### Quarterly Audit

- [ ] Review key rotation compliance (should be <90 days)
- [ ] Audit access logs for key vault
- [ ] Verify encryption configuration matches policy
- [ ] Test key rotation procedure in staging
- [ ] Review and update documentation
- [ ] Security team sign-off

### Annual Review

- [ ] Review encryption standards and algorithms
- [ ] Update TLS/cipher suite requirements
- [ ] Assess new encryption technologies
- [ ] Update incident response procedures
- [ ] Conduct tabletop exercise for key compromise scenario
- [ ] Review compliance with GDPR, PCI-DSS, etc.

## Troubleshooting

### Issue: Decryption Failures After Key Rotation

**Symptoms**:
- Error: "Encryption key version X not found"
- Unable to read encrypted data

**Resolution**:
```bash
# Verify all key versions are loaded
# Check application logs for key initialization

# Ensure old key versions are still accessible
az keyvault secret show \
  --vault-name <vault-name> \
  --name MercatoApp-MasterKey-v${OLD_VERSION}

# If old key is missing, restore from backup
az keyvault secret recover \
  --vault-name <vault-name> \
  --name MercatoApp-MasterKey-v${OLD_VERSION}
```

### Issue: High Encryption Latency

**Symptoms**:
- Slow response times for encrypted field operations
- Timeout errors

**Resolution**:
1. Check Key Vault/KMS latency
2. Verify application is caching derived keys
3. Consider increasing cache TTL
4. Monitor Key Vault throttling limits

### Issue: Certificate Issues

**Symptoms**:
- "NET::ERR_CERT_DATE_INVALID"
- "Certificate has expired"

**Resolution**:
```bash
# Check certificate expiration
openssl s_client -connect <domain>:443 -servername <domain> 2>/dev/null | \
  openssl x509 -noout -dates

# Renew certificate immediately
# See Certificate Expiration section above
```

## Contact Information

### Escalation Path

1. **Level 1**: DevOps Team (24/7)
   - Email: devops@mercatoapp.com
   - Slack: #devops-alerts

2. **Level 2**: Security Team
   - Email: security@mercatoapp.com
   - Phone: +1-XXX-XXX-XXXX
   - Slack: #security-incidents

3. **Level 3**: CISO / CTO
   - Emergency contact procedures in incident response plan

### External Resources

- Azure Support: [Azure Support Portal](https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade)
- AWS Support: [AWS Support Center](https://console.aws.amazon.com/support/)
- Certificate Authority Support: [Let's Encrypt Community](https://community.letsencrypt.org/)

## Appendix

### Useful Commands

```bash
# Check TLS version and cipher suites
nmap --script ssl-enum-ciphers -p 443 <domain>

# Verify HSTS header
curl -I https://<domain> | grep -i strict-transport-security

# Test HTTPS enforcement
curl -I http://<domain>
# Should return 301/302 redirect to HTTPS

# Validate certificate chain
openssl s_client -connect <domain>:443 -showcerts

# Check for weak ciphers
testssl.sh https://<domain>
```

### Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-03 | 1.0 | Initial runbook creation | Security Team |

---

**Document Classification**: Internal - Restricted  
**Last Updated**: 2025-12-03  
**Next Review**: 2026-03-03
