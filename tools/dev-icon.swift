// Writes AppIcon-Dev (the Debug build's icon) from AppIcon: every PNG with a DEV band across the lower part of the icon
// body, so the Dev app is obvious in the Dock, Launchpad and Finder. Deterministic; rerun after AppIcon changes.
//
// Usage (macOS):
//   swift deskling/tools/dev-icon.swift <Assets.xcassets> [band #RRGGBB] [text #RRGGBB]
//   (defaults: a dark brown band, cream letters)
import AppKit
import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

let arguments = CommandLine.arguments
guard arguments.count > 1 else {
    FileHandle.standardError.write(Data("usage: swift dev-icon.swift <Assets.xcassets> [band #RRGGBB] [text #RRGGBB]\n".utf8))
    exit(2)
}
let catalog = URL(fileURLWithPath: arguments[1])
let source = catalog.appendingPathComponent("AppIcon.appiconset")
let target = catalog.appendingPathComponent("AppIcon-Dev.appiconset")

/// "#RRGGBB" as sRGB components.
func rgb(_ hex: String) -> (CGFloat, CGFloat, CGFloat) {
    let digits = hex.hasPrefix("#") ? String(hex.dropFirst()) : hex
    guard digits.count == 6, let value = UInt32(digits, radix: 16) else {
        FileHandle.standardError.write(Data("not a #RRGGBB color: \(hex)\n".utf8))
        exit(2)
    }
    return (CGFloat((value >> 16) & 0xFF) / 255, CGFloat((value >> 8) & 0xFF) / 255, CGFloat(value & 0xFF) / 255)
}

let (br, bg, bb) = rgb(arguments.count > 2 ? arguments[2] : "#4A2E1A")
let (tr, tg, tb) = rgb(arguments.count > 3 ? arguments[3] : "#FFF8E7")
let band = CGColor(srgbRed: br, green: bg, blue: bb, alpha: 1)
let letters = NSColor(srgbRed: tr, green: tg, blue: tb, alpha: 1)

// The macOS icon grid on a 1024 canvas: an 824 body at 100 with 180 corners. The band sits in its lower part.
func stamp(_ image: CGImage) -> CGImage {
    let size = image.width
    let unit = CGFloat(size) / 1024
    let context = CGContext(
        data: nil, width: size, height: size, bitsPerComponent: 8, bytesPerRow: 0,
        space: CGColorSpace(name: CGColorSpace.sRGB)!,
        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    context.draw(image, in: CGRect(x: 0, y: 0, width: size, height: size))

    // CoreGraphics is bottom-up: the band spans 700…870 from the top.
    let body = CGPath(
        roundedRect: CGRect(x: 100 * unit, y: 100 * unit, width: 824 * unit, height: 824 * unit),
        cornerWidth: 180 * unit, cornerHeight: 180 * unit, transform: nil)
    let rect = CGRect(x: 100 * unit, y: (1024 - 870) * unit, width: 824 * unit, height: 170 * unit)
    context.saveGState()
    context.addPath(body)
    context.clip()
    context.setFillColor(band)
    context.fill(rect)
    context.restoreGState()

    // Tiny sizes keep just the band; the letters would be a smudge.
    if size >= 64 {
        let font = NSFont.systemFont(ofSize: 130 * unit, weight: .bold)
        let text = NSAttributedString(string: "DEV", attributes: [.font: font, .foregroundColor: letters, .kern: 24 * unit])
        let line = CTLineCreateWithAttributedString(text)
        let bounds = CTLineGetImageBounds(line, context)
        context.textPosition = CGPoint(x: rect.midX - bounds.width / 2 - bounds.minX, y: rect.midY - bounds.height / 2 - bounds.minY)
        CTLineDraw(line, context)
    }
    return context.makeImage()!
}

let files = try FileManager.default.contentsOfDirectory(atPath: source.path).filter { $0.hasSuffix(".png") }.sorted()
for name in files {
    guard let input = CGImageSourceCreateWithURL(source.appendingPathComponent(name) as CFURL, nil),
        let image = CGImageSourceCreateImageAtIndex(input, 0, nil)
    else { fatalError("Can't read \(name)") }
    let url = target.appendingPathComponent(name) as CFURL
    guard let output = CGImageDestinationCreateWithURL(url, UTType.png.identifier as CFString, 1, nil)
    else { fatalError("Can't write \(name)") }
    CGImageDestinationAddImage(output, stamp(image), nil)
    guard CGImageDestinationFinalize(output) else { fatalError("Can't write \(name)") }
}
print("Wrote \(files.count) icons to \(target.path)")
