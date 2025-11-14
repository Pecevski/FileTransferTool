# FileTransfer Tool

A robust, production-ready console application for secure and efficient file transfer with parallel block processing, integrity verification, and automatic retry mechanisms.

## Overview

The **FileTransfer Tool** is designed to transfer files reliably from a source location to a destination location with built-in error handling, data integrity verification, and parallel processing capabilities.
The tool divides files into manageable 1MB blocks and processes them concurrently using a configurable number of threads, ensuring fast and resilient transfers even for large files.

---

## Key Functionalities

### 1. **Parallel Block Transfer**
   - Files are automatically divided into **1MB blocks** for efficient processing
   - Supports **configurable thread concurrency** (1-32 threads) for parallel block transfer
   - Uses semaphore-based concurrency control to prevent resource exhaustion
   - Default: 2 threads

### 2. **Data Integrity Verification**
   - **Dual-hash verification** approach:
     - **MD5** hashing for individual blocks (fast, per-block verification)
     - **SHA256** hashing for complete file verification (cryptographic security)
   - Each transferred block is read back and compared with source using hash matching
   - Final full-file hash comparison ensures end-to-end integrity

### 3. **Automatic Retry Logic**
   - Up to **3 automatic retries** for failed blocks (configurable)
   - Exponential backoff: retry delay increases with each attempt (100ms × retry count)
   - Hash mismatch detection and reporting for diagnostics
   - Automatic cancellation of remaining operations on permanent failure

### 4. **Flexible Path Resolution**
   - Intelligent destination path handling:
     - Accepts existing directories, file paths, or drive roots
     - Auto-creates parent directories if needed
     - Validates and creates destination file with proper permissions
   - Supports both absolute and relative paths

### 5. **Progress Reporting**
   - Real-time transfer progress with block-level tracking
   - Displays successful/failed block counts
   - Shows transfer duration and performance metrics
   - Console-based color-coded status messages

### 6. **Transfer Summary & Verification Report**
   - Detailed transfer summary with success/failure statistics
   - Per-block checksums displayed in hexadecimal format
   - Offset and hash information for each block
   - Overall transfer status indication (✓ SUCCESS / ✗ FAILED)

### 7. **Multi-Transfer Sessions**
   - Users can perform multiple sequential transfers in a single session
   - Interactive prompts for retry, path re-entry, or application exit
   - Graceful error handling with clear messaging

### 8. **Cancellation Support**
   - Full support for `CancellationToken` propagation
   - Graceful shutdown of ongoing transfers
   - Cleanup of partial results on cancellation

---

## Technologies & Architecture

### Technology Stack

- **Language:** C# 12.0
- **Framework:** .NET 8
- **Architecture Pattern:** Clean Architecture with Dependency Injection
- **Concurrency Model:** Async/Await with `Task Parallel Library (TPL)`
- **Hashing Algorithms:** MD5, SHA256 (.NET built-in cryptography)

### Project Structure

The application follows **Clean Architecture** principles with clear separation of concerns:
