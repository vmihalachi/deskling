#if os(macOS)
    import DesklingStore
    import DesklingTesting
    import XCTest

    /// `MockStoreBackend`'s purchase, restore and failure paths, and `send` reaching the observer.
    @MainActor
    final class MockStoreBackendTests: XCTestCase {
        private let supporter = "test.supporter"
        private let tip = "test.tip"

        private func makeBackend() -> MockStoreBackend {
            MockStoreBackend(catalog: ProductCatalog(unlockables: [supporter], tips: [tip]))
        }

        func testListsEveryProductPricedByItsIdsLength() async throws {
            let products = try await makeBackend().loadProducts([supporter, tip])
            XCTAssertEqual(products.map(\.id), [supporter, tip])
            XCTAssertEqual(products.map(\.displayPrice), ["€14", "€8"])
        }

        func testMissingProductsAreLeftOut() async throws {
            let backend = makeBackend()
            backend.missingProducts = [tip]
            let products = try await backend.loadProducts([supporter, tip])
            XCTAssertEqual(products.map(\.id), [supporter])
        }

        func testLoadingCanFail() async {
            let backend = makeBackend()
            backend.failProducts = true
            await assertFails { _ = try await backend.loadProducts([supporter]) }
        }

        func testBuyingAnUnlockableMakesItOwned() async throws {
            let backend = makeBackend()
            let outcome = try await backend.purchase(supporter)
            XCTAssertEqual(outcome, .purchased)
            XCTAssertEqual(backend.purchased, [supporter])
            let owned = await backend.entitledProductIDs()
            XCTAssertEqual(owned, [supporter])
        }

        func testBuyingATipOwnsNothing() async throws {
            let backend = makeBackend()
            let outcome = try await backend.purchase(tip)
            XCTAssertEqual(outcome, .purchased)
            XCTAssertEqual(backend.purchased, [tip])
            XCTAssertEqual(backend.owned, [])
        }

        func testAPendingOrCancelledPurchaseOwnsNothing() async throws {
            let backend = makeBackend()
            backend.nextOutcome = .pending
            let pending = try await backend.purchase(supporter)
            backend.nextOutcome = .cancelled
            let cancelled = try await backend.purchase(supporter)
            XCTAssertEqual([pending, cancelled], [.pending, .cancelled])
            XCTAssertEqual(backend.purchased, [supporter, supporter])
            XCTAssertEqual(backend.owned, [])
        }

        func testAFailedPurchaseIsNotRecorded() async {
            let backend = makeBackend()
            backend.failPurchase = true
            await assertFails { _ = try await backend.purchase(supporter) }
            XCTAssertEqual(backend.purchased, [])
            XCTAssertEqual(backend.owned, [])
        }

        func testRestoresAreCountedUntilTheyFail() async throws {
            let backend = makeBackend()
            try await backend.restore()
            try await backend.restore()
            XCTAssertEqual(backend.restores, 2)
            backend.failRestore = true
            await assertFails { try await backend.restore() }
            XCTAssertEqual(backend.restores, 2)
        }

        func testSendReachesTheObserverOnceThereIsOne() {
            let backend = makeBackend()
            let refund = TransactionUpdate(productID: supporter, isRevoked: true)
            var received: [TransactionUpdate] = []
            backend.send(refund)
            backend.observeTransactions { received.append($0) }
            backend.send(refund)
            XCTAssertEqual(received, [refund])
        }

        /// The call throws `MockStoreBackend.Failure`.
        private func assertFails(_ body: () async throws -> Void, file: StaticString = #filePath, line: UInt = #line) async {
            do {
                try await body()
                XCTFail("expected MockStoreBackend.Failure", file: file, line: line)
            } catch {
                XCTAssertTrue(error is MockStoreBackend.Failure, "\(error)", file: file, line: line)
            }
        }
    }
#endif
