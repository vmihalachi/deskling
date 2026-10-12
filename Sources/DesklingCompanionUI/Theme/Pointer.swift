#if os(macOS)
    import SwiftUI

    /// Home's pointer toward the menu bar: a dashed curve to the top right with a chevron at its end.
    public struct Pointer: View {
        let color: Color
        let lineWidth: CGFloat
        let headLength: CGFloat

        /// `headLength` is how far the chevron's arms reach back from the tip.
        public init(color: Color, lineWidth: CGFloat = 2, headLength: CGFloat = 11) {
            self.color = color
            self.lineWidth = lineWidth
            self.headLength = headLength
        }

        public var body: some View {
            ZStack {
                PointerCurve().stroke(color, style: StrokeStyle(lineWidth: lineWidth, lineCap: .round, dash: [4, 5]))
                PointerHead(length: headLength)
                    .stroke(color, style: StrokeStyle(lineWidth: lineWidth, lineCap: .round, lineJoin: .round))
            }
            .accessibilityHidden(true)
        }

        private struct PointerCurve: SwiftUI.Shape {
            func path(in rect: CGRect) -> Path {
                var path = Path()
                path.move(to: CGPoint(x: rect.minX, y: rect.maxY))
                path.addQuadCurve(
                    to: CGPoint(x: rect.maxX - 2, y: rect.minY + 2),
                    control: CGPoint(x: rect.minX + rect.width * 0.15, y: rect.minY + rect.height * 0.1))
                return path
            }
        }

        private struct PointerHead: SwiftUI.Shape {
            let length: CGFloat

            func path(in rect: CGRect) -> Path {
                var path = Path()
                let tip = CGPoint(x: rect.maxX - 2, y: rect.minY + 2)
                path.move(to: CGPoint(x: tip.x - length, y: tip.y + 1))
                path.addLine(to: tip)
                path.addLine(to: CGPoint(x: tip.x - 3, y: tip.y + length))
                return path
            }
        }
    }
#endif
