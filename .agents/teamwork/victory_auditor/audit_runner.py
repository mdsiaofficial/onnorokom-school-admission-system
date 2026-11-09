#!/usr/bin/env python3
import json
import urllib.request
import urllib.error
import concurrent.futures
import sys
import time

BASE_URL = "http://localhost:8080"

def request(method, path, headers=None, body=None):
    url = f"{BASE_URL}{path}"
    req_headers = {"Content-Type": "application/json"}
    if headers:
        req_headers.update(headers)
    
    data = json.dumps(body).encode('utf-8') if body is not None else None
    req = urllib.request.Request(url, data=data, headers=req_headers, method=method)
    try:
        with urllib.request.urlopen(req) as resp:
            resp_body = resp.read().decode('utf-8')
            return resp.status, resp.headers, json.loads(resp_body) if resp_body else None
    except urllib.error.HTTPError as e:
        err_body = e.read().decode('utf-8')
        try:
            parsed = json.loads(err_body)
        except Exception:
            parsed = err_body
        return e.code, e.headers, parsed

def test_api_contracts():
    print("[TEST] 1. API Contract & Validation Verification...")
    
    # Missing header
    status, _, data = request("POST", "/classes/00000000-0000-0000-0000-000000000000/holds")
    assert status == 400, f"Expected 400 for missing header, got {status}"
    assert data.get("code") == "INVALID_STUDENT_ID", f"Expected INVALID_STUDENT_ID, got {data}"
    
    # 16-digit header
    status, _, data = request("POST", "/classes/00000000-0000-0000-0000-000000000000/holds", headers={"X-Student-Id": "1234567890123456"})
    assert status == 400, f"Expected 400 for 16-digit header, got {status}"
    
    # Non-digit header
    status, _, data = request("POST", "/classes/00000000-0000-0000-0000-000000000000/holds", headers={"X-Student-Id": "1234567890123456A"})
    assert status == 400, f"Expected 400 for non-digit header, got {status}"
    
    # Create class
    class_payload = {"name": f"Audit-Class-{int(time.time())}", "totalSeats": 5}
    status, _, cls_data = request("POST", "/classes", body=class_payload)
    assert status == 201, f"Expected 201 for POST /classes, got {status}: {cls_data}"
    assert "id" in cls_data and cls_data["name"] == class_payload["name"] and cls_data["totalSeats"] == 5
    class_id = cls_data["id"]
    print(f"   Created class: {class_id}")
    
    # GET /classes
    status, _, classes = request("GET", "/classes")
    assert status == 200, f"Expected 200 for GET /classes, got {status}"
    found = next((c for c in classes if c["id"] == class_id), None)
    assert found is not None, f"Created class not in GET /classes"
    assert found["totalSeats"] == 5 and found["heldSeats"] == 0 and found["confirmedSeats"] == 0 and found["availableSeats"] == 5
    
    # GET /classes/{id}
    status, _, cls_detail = request("GET", f"/classes/{class_id}")
    assert status == 200, f"Expected 200 for GET /classes/{class_id}, got {status}"
    assert cls_detail["availableSeats"] == 5
    
    # POST /classes/{id}/holds (valid 17-digit student ID)
    student1 = "99990000000000001"
    status, _, hold_data = request("POST", f"/classes/{class_id}/holds", headers={"X-Student-Id": student1})
    assert status == 201, f"Expected 201 for hold creation, got {status}: {hold_data}"
    assert hold_data["studentId"] == student1
    assert hold_data["status"] == "HELD"
    assert "holdId" in hold_data and "expiresAt" in hold_data
    hold1_id = hold_data["holdId"]
    print(f"   Created hold {hold1_id} for student {student1}")
    
    # Duplicate hold for same student in same class -> Conflict
    status, _, dup_data = request("POST", f"/classes/{class_id}/holds", headers={"X-Student-Id": student1})
    assert status == 409, f"Expected 409 for duplicate active hold, got {status}: {dup_data}"
    assert dup_data.get("code") == "ALREADY_HELD", f"Expected ALREADY_HELD, got {dup_data}"
    
    # GET /holds/{holdId}
    status, _, hold_status = request("GET", f"/holds/{hold1_id}")
    assert status == 200, f"Expected 200 for GET /holds/{hold1_id}, got {status}"
    assert hold_status["holdId"] == hold1_id and hold_status["status"] == "HELD"
    
    # GET /me/holds for student1
    status, _, my_holds = request("GET", "/me/holds", headers={"X-Student-Id": student1})
    assert status == 200, f"Expected 200 for GET /me/holds, got {status}"
    assert any(h["holdId"] == hold1_id for h in my_holds)
    
    # Student 2 tries to confirm student 1's hold -> 403 Forbidden
    student2 = "99990000000000002"
    status, _, forbidden_data = request("POST", f"/holds/{hold1_id}/confirm", headers={"X-Student-Id": student2})
    assert status == 403, f"Expected 403 Forbidden when student 2 confirms student 1's hold, got {status}"
    
    # Student 1 confirms own hold -> 200 OK
    status, _, confirm_data = request("POST", f"/holds/{hold1_id}/confirm", headers={"X-Student-Id": student1})
    assert status == 200, f"Expected 200 for hold confirmation, got {status}: {confirm_data}"
    assert confirm_data["status"] == "CONFIRMED"
    
    # Idempotent confirmation: student 1 confirms again -> 200 OK
    status, _, confirm2_data = request("POST", f"/holds/{hold1_id}/confirm", headers={"X-Student-Id": student1})
    assert status == 200, f"Expected 200 for idempotent confirmation, got {status}: {confirm2_data}"
    assert confirm2_data["status"] == "CONFIRMED"
    
    # Verify counts: totalSeats: 5, heldSeats: 0, confirmedSeats: 1, availableSeats: 4
    status, _, cls_detail = request("GET", f"/classes/{class_id}")
    assert cls_detail["totalSeats"] == 5
    assert cls_detail["heldSeats"] == 0
    assert cls_detail["confirmedSeats"] == 1
    assert cls_detail["availableSeats"] == 4
    
    # Cancellation of confirmed hold -> 409 Conflict
    status, _, cancel_conf_data = request("DELETE", f"/holds/{hold1_id}", headers={"X-Student-Id": student1})
    assert status == 409, f"Expected 409 when cancelling confirmed hold, got {status}: {cancel_conf_data}"
    
    # Test Cancellation of active hold:
    status, _, hold2_data = request("POST", f"/classes/{class_id}/holds", headers={"X-Student-Id": student2})
    assert status == 201, f"Expected 201 for student 2 hold, got {status}"
    hold2_id = hold2_data["holdId"]
    
    # Check counts before cancel: heldSeats: 1, confirmedSeats: 1, availableSeats: 3
    status, _, cls_detail = request("GET", f"/classes/{class_id}")
    assert cls_detail["heldSeats"] == 1 and cls_detail["availableSeats"] == 3
    
    # Cancel hold 2: DELETE /holds/{holdId}
    status, _, _ = request("DELETE", f"/holds/{hold2_id}", headers={"X-Student-Id": student2})
    assert status == 204, f"Expected 204 for DELETE /holds/{hold2_id}, got {status}"
    
    # Check counts after cancel: heldSeats: 0, availableSeats: 4
    status, _, cls_detail = request("GET", f"/classes/{class_id}")
    assert cls_detail["heldSeats"] == 0 and cls_detail["availableSeats"] == 4
    
    print("[PASS] API Contract & Validation passed 100%!")
    return class_id

def test_concurrency_race():
    print("\n[TEST] 2. Concurrency Invariant & Overselling Prevention Test...")
    # Create class with totalSeats = 6
    class_payload = {"name": f"Concurrency-Stress-{int(time.time())}", "totalSeats": 6}
    status, _, cls_data = request("POST", "/classes", body=class_payload)
    assert status == 201
    class_id = cls_data["id"]
    print(f"   Created test class '{class_payload['name']}' with 6 seats (id: {class_id})")
    
    # Generate 40 distinct 17-digit student IDs
    num_requests = 40
    student_ids = [f"8888{i:013d}" for i in range(1, num_requests + 1)]
    
    results = []
    
    def post_hold(s_id):
        return request("POST", f"/classes/{class_id}/holds", headers={"X-Student-Id": s_id})
    
    start_time = time.time()
    with concurrent.futures.ThreadPoolExecutor(max_workers=40) as executor:
        futures = [executor.submit(post_hold, s_id) for s_id in student_ids]
        for f in concurrent.futures.as_completed(futures):
            results.append(f.result())
    elapsed = time.time() - start_time
    
    statuses = [r[0] for r in results]
    success_count = statuses.count(201)
    conflict_count = statuses.count(409)
    other_count = len(statuses) - (success_count + conflict_count)
    
    print(f"   Completed {num_requests} concurrent requests in {elapsed:.3f}s:")
    print(f"   - 201 Created (Success): {success_count}")
    print(f"   - 409 Conflict (Seats Full): {conflict_count}")
    print(f"   - Other status codes: {other_count}")
    
    assert other_count == 0, f"Unexpected status codes returned: {[r for r in results if r[0] not in (201, 409)]}"
    assert success_count == 6, f"VIOLATION: Expected exactly 6 successful holds, got {success_count}!"
    assert conflict_count == 34, f"VIOLATION: Expected exactly 34 conflicts, got {conflict_count}!"
    
    # Query GET /classes/{classId}
    status, _, cls_detail = request("GET", f"/classes/{class_id}")
    assert cls_detail["totalSeats"] == 6
    assert cls_detail["heldSeats"] == 6
    assert cls_detail["confirmedSeats"] == 0
    assert cls_detail["availableSeats"] == 0
    
    print(f"   Verified class status: total=6, held=6, confirmed=0, available=0")
    print("[PASS] Concurrency Invariant holds! Zero overselling.")

if __name__ == "__main__":
    test_api_contracts()
    test_concurrency_race()
    print("\nALL AUDIT TESTS COMPLETED SUCCESSFULLY!")
