// Copyright (C) NeoAxis Group Ltd. 8 Copthall, Roseau Valley, 00152 Commonwealth of Dominica.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Windows.Forms;
using System.Drawing;
using NeoAxis.Editor;
using NeoAxis.Widget;
using ComponentFactory.Krypton.Toolkit;

namespace NeoAxis.Addon.EditorExample
{
	public partial class Component_EditorExampleCanvas_DocumentWindow : DocumentWindowWithViewport
	{
		//EngineFont nodeFont;

		static float[] zoomTable = new float[] { .1f, .2f, .35f, .5f, .6f, .7f, .8f, .9f, 1, 1.1f, 1.2f, 1.3f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f };

		//depending system DPI
		float cellSize;

		//Scrolling
		bool scrollView_Enabled;
		bool scrollView_Activated;
		Vector2 scrollView_StartScrollPosition;
		Vector2I scrollView_StartMousePositionInPixels;
		Vector2 scrollView_StartMousePositionInScreen;

		//Select by rectangle
		bool selectByRectangle_Enabled;
		bool selectByRectangle_Activated;
		Vector2 selectByRectangle_StartPosInScreen;
		Vector2 selectByRectangle_StartPosInUnits;
		Vector2 selectByRectangle_LastMousePositionInUnits;

		////node move
		//bool nodeMove_Enabled;
		//bool nodeMove_Activated;
		//bool nodeMove_Cloned;
		//Vec2I nodeMove_StartMousePositionInPixels;
		//Vec2 nodeMove_StartMousePositionInUnits;
		//Component_FlowchartNode nodeMove_StartOverNode;
		////!!!!��������� �� ��������� ��. ��� ��������� ��������. ��� ��� ���
		//ESet<Component_FlowchartNode> nodeMove_Nodes;
		//Dictionary<Component_FlowchartNode, Vec2I> nodeMove_StartPositions;

		////drag and drop
		//Component dragDropObject;
		////!!!!
		//DragDropSetReferenceData dragDropSetReferenceData;
		//bool dragDropSetReferenceDataCanSet;
		//string[] dragDropSetReferenceDataCanSetReferenceValues;

		///////////////////////////////////////////

		public Component_EditorExampleCanvas_DocumentWindow()
		{
			InitializeComponent();

			if( EditorUtility.IsDesignerHosted( this ) )
				return;

			CalculateCellSize();
		}

		[Browsable( false )]
		public Component_EditorExampleCanvas EditorExampleCanvas
		{
			get { return ObjectOfWindow as Component_EditorExampleCanvas; }
		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			//if( ObjectOfWindow != null )
			//	SelectObjects( new object[] { ObjectOfWindow } );
		}

		public float GetZoom()
		{
			if( EditorExampleCanvas.EditorZoomIndex >= 0 && EditorExampleCanvas.EditorZoomIndex < zoomTable.Length )
				return zoomTable[ EditorExampleCanvas.EditorZoomIndex ];
			return 1;
		}

		double GetScreenCellSizeX()
		{
			return (double)cellSize / (double)ViewportControl.Viewport.SizeInPixels.X;
		}

		double GetScreenCellSizeY()
		{
			return (double)cellSize / (double)ViewportControl.Viewport.SizeInPixels.Y;
		}

		double GetEditorScrollPositionX()
		{
			return EditorExampleCanvas.EditorScrollPosition.X - ConvertScreenToUnitX( 0.5, false );
		}

		double GetEditorScrollPositionY()
		{
			return EditorExampleCanvas.EditorScrollPosition.Y - ConvertScreenToUnitY( 0.5, false );
		}

		public double ConvertUnitToScreenX( double posX )
		{
			double screen = ( posX - GetEditorScrollPositionX() ) * GetScreenCellSizeX();
			screen *= GetZoom();
			return screen;
		}

		public double ConvertUnitToScreenY( double posY )
		{
			double screen = ( posY - GetEditorScrollPositionY() ) * GetScreenCellSizeY();
			screen *= GetZoom();
			return screen;
		}

		public Vector2 ConvertUnitToScreen( Vector2 vector )
		{
			return new Vector2(
				ConvertUnitToScreenX( vector.X ),
				ConvertUnitToScreenY( vector.Y ) );
		}

		public Rectangle ConvertUnitToScreen( Rectangle rect )
		{
			return new Rectangle(
				ConvertUnitToScreenX( rect.Left ),
				ConvertUnitToScreenY( rect.Top ),
				ConvertUnitToScreenX( rect.Right ),
				ConvertUnitToScreenY( rect.Bottom ) );
		}

		public double ConvertScreenToUnitX( double screenX, bool applyScrollPosition )
		{
			double v = screenX / GetScreenCellSizeX() / GetZoom();
			if( applyScrollPosition )
				v += GetEditorScrollPositionX();
			return v;
		}

		public double ConvertScreenToUnitY( double screenY, bool applyScrollPosition )
		{
			double v = screenY / GetScreenCellSizeY() / GetZoom();
			if( applyScrollPosition )
				v += GetEditorScrollPositionY();
			return v;
		}

		public Vector2 ConvertScreenToUnit( Vector2 screen, bool applyScrollPosition )
		{
			return new Vector2(
				ConvertScreenToUnitX( screen.X, applyScrollPosition ),
				ConvertScreenToUnitY( screen.Y, applyScrollPosition ) );
		}

		public RectangleI GetVisibleCells()
		{
			Vector2I from = ConvertScreenToUnit( Vector2.Zero, true ).ToVector2I() - new Vector2I( 1, 1 );
			Vector2I to = ConvertScreenToUnit( Vector2.One, true ).ToVector2I() + new Vector2I( 1, 1 );
			return new RectangleI( from, to );
		}

		public Rectangle SelectByRectangle_GetRectangleInUnits()
		{
			Rectangle rect = new Rectangle( selectByRectangle_StartPosInUnits );
			rect.Add( selectByRectangle_LastMousePositionInUnits );
			return rect;
		}

		//void UpdateFontSize( CanvasRenderer renderer )
		//{
		//	int height = renderer.ViewportForScreenCanvasRenderer.SizeInPixels.Y;
		//	float screenCellSize = (float)cellSize / (float)height;
		//	float demandFontHeight = screenCellSize * GetZoom();

		//	if( nodeFont == null || nodeFont.Height != demandFontHeight )
		//	{
		//		nodeFont = EngineFontManager.Instance.LoadFont( "FlowchartEditor", demandFontHeight );
		//		//nodeFontValue = EngineFontManager.Instance.LoadFont( "FlowchartEditor", demandFontHeight * .8f );
		//	}
		//}

		void ShowContextMenu()
		{
			var items = new List<KryptonContextMenuItemBase>();

			Component oneSelectedComponent = null;
			if( SelectedObjects.Length == 1 )
				oneSelectedComponent = SelectedObjects[ 0 ] as Component;

			////Editor
			//{
			//	var item = new KryptonContextMenuItem( Translate( "Editor" ), Properties.Resources.Edit_16, delegate ( object s, EventArgs e2 )
			//	 {
			//		 bool canUseAlreadyOpened = !ModifierKeys.HasFlag( Keys.Shift );
			//		 EditorForm.Instance.OpenDocumentWindowForObject( Document, oneSelectedComponent, canUseAlreadyOpened );
			//	 } );
			//	item.Enabled = oneSelectedComponent != null &&
			//		EditorForm.Instance.CanOpenDocumentWindowForObject( Document, oneSelectedComponent );
			//	items.Add( item );
			//}

			////Settings
			//{
			//	var item = new KryptonContextMenuItem( Translate( "Settings" ), Properties.Resources.Maximize_16, delegate ( object s, EventArgs e2 )
			//	{
			//		bool canUseAlreadyOpened = !ModifierKeys.HasFlag( Keys.Shift );
			//		EditorForm.Instance.ShowObjectSettingsWindow( Document, oneSelectedComponent, canUseAlreadyOpened );
			//	} );
			//	item.Enabled = oneSelectedComponent != null;
			//	items.Add( item );
			//}

			//items.Add( new KryptonContextMenuSeparator() );

			//New object
			{
				KryptonContextMenuItem item = new KryptonContextMenuItem( Translate( "New Object" ), EditorResourcesCache.New, delegate ( object s, EventArgs e2 )
				{
					TryNewObject();
				} );
				item.Enabled = CanNewObject( out List<Component> dummy2 );
				items.Add( item );
			}

			//Clone
			{
				var item = new KryptonContextMenuItem( Translate( "Duplicate" ), EditorResourcesCache.Clone, delegate ( object s, EventArgs e2 )
				{
					EditorAPI.EditorActionClick( EditorAction.HolderEnum.ContextMenu, "Duplicate" );
				} );
				item.ShortcutKeyDisplayString = EditorActions.GetFirstShortcutKeyString( "Duplicate" );
				item.Enabled = EditorAPI.EditorActionGetState( EditorAction.HolderEnum.ContextMenu, "Duplicate" ).Enabled;
				items.Add( item );
			}

			//Delete
			{
				var item = new KryptonContextMenuItem( Translate( "Delete" ), EditorResourcesCache.Delete, delegate ( object s, EventArgs e2 )
				{
					EditorAPI.EditorActionClick( EditorAction.HolderEnum.ContextMenu, "Delete" );
				} );
				item.Enabled = EditorAPI.EditorActionGetState( EditorAction.HolderEnum.ContextMenu, "Delete" ).Enabled;
				items.Add( item );
			}

			//Rename
			{
				var item = new KryptonContextMenuItem( Translate( "Rename" ), null, delegate ( object s, EventArgs e2 )
				{
					EditorUtility.ShowRenameComponentDialog( oneSelectedComponent );
				} );
				item.ShortcutKeyDisplayString = EditorActions.GetFirstShortcutKeyString( "Rename" );
				item.Enabled = oneSelectedComponent != null;
				items.Add( item );
			}

			EditorContextMenu.AddActionsToMenu( EditorContextMenu.MenuTypeEnum.Document, items );//, this );

			EditorContextMenu.Show( items, this );
		}

		object GetMouseOverObject()
		{
			//foreach( Component_FlowchartNode node in EditorExampleCanvas.GetComponents<Component_FlowchartNode>( true ) )
			//{
			//	var style = node.GetResultStyle( EditorExampleCanvas );

			//	var obj = style.GetMouseOverObject( this, node );
			//	if( obj != null )
			//		return obj;
			//}

			return null;
		}

		//!!!!
		string Translate( string text )
		{
			return text;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected override void ViewportControl_ViewportCreated( EngineViewportControl sender )
		{
			base.ViewportControl_ViewportCreated( sender );
		}

		protected override void Viewport_KeyDown( Viewport viewport, KeyEvent e, ref bool handled )
		{
			base.Viewport_KeyDown( viewport, e, ref handled );
			if( handled )
				return;
		}

		protected override void Viewport_KeyPress( Viewport viewport, KeyPressEvent e, ref bool handled )
		{
			base.Viewport_KeyPress( viewport, e, ref handled );
			if( handled )
				return;
		}

		protected override void Viewport_KeyUp( Viewport viewport, KeyEvent e, ref bool handled )
		{
			base.Viewport_KeyUp( viewport, e, ref handled );
		}

		protected override void Viewport_MouseDown( Viewport viewport, EMouseButtons button, ref bool handled )
		{
			base.Viewport_MouseDown( viewport, button, ref handled );
			if( handled )
				return;

			if( button == EMouseButtons.Left )
			{
				var mouseOverObject = GetMouseOverObject();
				//var mouseOverNode = mouseOverObject as Component_FlowchartNode;

				//if( mouseOverNode != null )
				//{
				//	//if( IsSelectableObject( obj ) )
				//	//{

				//	//Component_FlowchartNode node = node as Component_FlowchartNode;
				//	//if( node != null )
				//	//{

				//	//prepare to node moving

				//	Vec2 mouse = viewport.MousePosition;
				//	Vec2I mouseInPixels = ( mouse * viewport.SizeInPixels.ToVec2() ).ToVec2I();

				//	//nodeMove_Enabled = true;
				//	//nodeMove_Activated = false;
				//	//nodeMove_Cloned = false;
				//	//nodeMove_StartOverNode = mouseOverNode;
				//	//nodeMove_StartMousePositionInPixels = mouseInPixels;
				//	//nodeMove_StartMousePositionInUnits = ConvertScreenToUnit( mouse, true );

				//	//nodeMove_Nodes = new ESet<Component_FlowchartNode>();
				//	//nodeMove_Nodes.Add( mouseOverNode );

				//	//var objectToNodes = GetObjectToNodesDictionary();

				//	//foreach( var selectedObject in SelectedObjectsSet )
				//	//{
				//	//	var n = selectedObject as Component_FlowchartNode;
				//	//	if( n != null )
				//	//		nodeMove_Nodes.AddWithCheckAlreadyContained( n );

				//	//	var c = selectedObject as Component;
				//	//	if( c != null )
				//	//	{
				//	//		if( objectToNodes.TryGetValue( c, out List<Component_FlowchartNode> nodes ) )
				//	//		{
				//	//			foreach( var n2 in nodes )
				//	//			{
				//	//				var startNodeObject = nodeMove_StartOverNode.ControlledObject.Value;
				//	//				if( startNodeObject != null && n2.ControlledObject.Value == startNodeObject )
				//	//					continue;

				//	//				nodeMove_Nodes.AddWithCheckAlreadyContained( n2 );
				//	//			}
				//	//		}
				//	//	}
				//	//}

				//	//nodeMove_StartPositions = new Dictionary<Component_FlowchartNode, Vec2I>();
				//	//foreach( var n in nodeMove_Nodes )
				//	//	nodeMove_StartPositions.Add( n, n.NodePosition );

				//	handled = true;
				//	return;
				//}
				//else if( mouseOverSocket != null )
				//{
				//	referenceCreationSocketFrom = mouseOverSocket;

				//	//!!!!����
				//	//}
				//	//else
				//	//{
				//	//	//!!!!

				//	//	var socket = obj as Component_FlowchartNode.Representation.Socket;
				//	//	if( socket != null )
				//	//	{
				//	//		referenceCreationSocketFrom = socket;
				//	//	}
				//	//	else
				//	//	{
				//	//		PinInputMouseSelection pinSelection = obj as PinInputMouseSelection;
				//	//		//!!!!����
				//	//		//if( pinSelection != null )
				//	//		//	EditPinValue( pinSelection.pin, e.Location );
				//	//	}
				//	//}

				//	handled = true;
				//	return;
				//}
				//else
				{
					//activate selection by rectangle
					selectByRectangle_Enabled = true;
					selectByRectangle_Activated = false;
					selectByRectangle_StartPosInScreen = viewport.MousePosition;
					selectByRectangle_StartPosInUnits = ConvertScreenToUnit( selectByRectangle_StartPosInScreen, true );
					selectByRectangle_LastMousePositionInUnits = selectByRectangle_StartPosInUnits;

					handled = true;
					return;
				}
			}

			//scroll view
			if( button == EMouseButtons.Right )
			{
				Vector2 mouse = viewport.MousePosition;
				Vector2I mouseInPixels = ( mouse * viewport.SizeInPixels.ToVector2() ).ToVector2I();

				scrollView_Enabled = true;
				scrollView_Activated = false;
				scrollView_StartScrollPosition = EditorExampleCanvas.EditorScrollPosition;
				scrollView_StartMousePositionInPixels = mouseInPixels;
				scrollView_StartMousePositionInScreen = mouse;

				handled = true;
				return;
			}
		}

		protected override void Viewport_MouseUp( Viewport viewport, EMouseButtons button, ref bool handled )
		{
			//inside: cameraRotation
			base.Viewport_MouseUp( viewport, button, ref handled );

			//below:
			//nodeMove
			//scrollView
			//select by rectangle
			//context menu

			object mouseOverObject = GetMouseOverObject();
			var selectedObjects = new ESet<object>( SelectedObjectsSet );

			if( button == EMouseButtons.Left )
			{
				////update selected objects
				//bool allowSelect = true;

				////!!!!��� ��� � ����� ����� �������. �����, ���� �� �������, �� ��������
				//if( nodeMove_Activated  )
				//	allowSelect = false;
				//if( allowSelect )
				//{
				//	bool shiftPressed = ( Form.ModifierKeys & Keys.Shift ) != 0;
				//	if( !shiftPressed )
				//		selectedObjects.Clear();

				//	//select node
				//	var node = mouseOverObject as Component_FlowchartNode;
				//	if( node != null )
				//	{
				//		var obj = node.ControlledObject.Value;
				//		if( obj != null )
				//		{
				//			if( !selectedObjects.Contains( obj ) )
				//				selectedObjects.Add( obj );
				//			else
				//				selectedObjects.Remove( obj );
				//		}
				//		else
				//		{
				//			//select node without assigned controlled object
				//			selectedObjects.Add( node );
				//		}
				//	}
				//}

				////node move
				//if( nodeMove_Activated )
				//{
				//	//undo

				//	//!!!!!!��������, ���� �������� �� Escape

				//	if( !nodeMove_Cloned )
				//	{
				//		//changed

				//		var undoItems = new List<UndoActionPropertiesChange.Item>();

				//		foreach( var node in nodeMove_Nodes )
				//		{
				//			var oldValue = nodeMove_StartPositions[ node ];
				//			var property = (Metadata.Property)MetadataManager.GetTypeOfNetType( node.GetType() ).
				//				MetadataGetMemberBySignature( "property:Position" );
				//			var undoItem = new UndoActionPropertiesChange.Item( node, property, oldValue, null );
				//			undoItems.Add( undoItem );
				//		}

				//		if( undoItems.Count != 0 )
				//		{
				//			var action = new UndoActionPropertiesChange( undoItems.ToArray() );
				//			Document.UndoSystem.CommitAction( action );
				//			Document.Modified = true;
				//		}
				//	}
				//	else
				//	{
				//		//cloned
				//		var action = new UndoActionComponentCreateDelete( this, nodeMove_Nodes.ToArray(), true );
				//		Document.UndoSystem.CommitAction( action );
				//		Document.Modified = true;
				//	}
				//}
				//nodeMove_Enabled = false;
				//nodeMove_Activated = false;
				//nodeMove_Cloned = false;
				//nodeMove_StartOverNode = null;
				//nodeMove_Nodes = null;
				//nodeMove_StartPositions = null;
				//nodeMove_StartMousePositionInPixels = Vec2I.Zero;
				//nodeMove_StartMousePositionInUnits = Vec2.Zero;
			}

			//select by rectangle
			if( button == EMouseButtons.Left )
			{
				if( selectByRectangle_Enabled )
				{
					if( selectByRectangle_Activated )//!!!!new
					{
						bool shiftPressed = ( Form.ModifierKeys & Keys.Shift ) != 0;
						if( !shiftPressed )
							selectedObjects.Clear();

						if( selectByRectangle_Activated )
						{
							//foreach( var node in SelectByRectangle_GetNodes() )
							//	selectedObjects.AddWithCheckAlreadyContained( node );
						}

						if( selectByRectangle_Activated )
							handled = true;
					}

					selectByRectangle_Enabled = false;
					selectByRectangle_Activated = false;
				}
			}

			//scroll view
			if( button == EMouseButtons.Right )
			{
				if( scrollView_Activated )
					handled = true;

				scrollView_Enabled = false;
				scrollView_Activated = false;
			}

			//update selected objects
			SelectObjects( selectedObjects );

			//context menu
			if( !handled && button == EMouseButtons.Right )
				ShowContextMenu();
		}

		protected override void Viewport_MouseDoubleClick( Viewport viewport, EMouseButtons button, ref bool handled )
		{
			base.Viewport_MouseDoubleClick( viewport, button, ref handled );
			if( handled )
				return;
		}

		protected override void Viewport_MouseMove( Viewport viewport, Vector2 mouse )//, ref bool handled )
		{
			base.Viewport_MouseMove( viewport, mouse );//, ref handled );

			//!!!!!handled

			////!!!!
			//if( nodeMove_Enabled && !nodeMove_Activated )
			//{
			//	Vec2I mouseInPixels = ( mouse * viewport.SizeInPixels.ToVec2() ).ToVec2I();
			//	Vec2I diff = nodeMove_StartMousePositionInPixels - mouseInPixels;
			//	if( Math.Abs( diff.X ) > 2 || Math.Abs( diff.Y ) > 2 )
			//	{
			//		nodeMove_Activated = true;

			//		//Clone
			//		if( ( ModifierKeys & Keys.Shift ) != 0 )//if( EngineApp.Instance.IsKeyPressed( EKeys.Shift ) )
			//		{
			//			Component_FlowchartNode nodeMove_StartOverNodeSource = nodeMove_StartOverNode;
			//			ESet<Component_FlowchartNode> nodeMove_NodesSource = nodeMove_Nodes;
			//			Dictionary<Component_FlowchartNode, Vec2I> nodeMove_StartPositionsSource = nodeMove_StartPositions;

			//			nodeMove_StartOverNode = null;
			//			nodeMove_Nodes = new ESet<Component_FlowchartNode>();
			//			nodeMove_StartPositions = new Dictionary<Component_FlowchartNode, Vec2I>();

			//			//!!!!����� ���-�� ����� �����������, ����� ������ ��������?

			//			foreach( var sourceNode in nodeMove_NodesSource )
			//			{
			//				var node = (Component_FlowchartNode)EditorUtils.CloneComponent( sourceNode );

			//				if( nodeMove_StartOverNodeSource == sourceNode )
			//					nodeMove_StartOverNode = node;
			//				nodeMove_Nodes.Add( node );
			//				nodeMove_StartPositions[ node ] = nodeMove_StartPositionsSource[ sourceNode ];
			//			}

			//			nodeMove_Cloned = true;

			//			SelectObjects( nodeMove_Nodes.ToArray() );

			//			//add screen message
			//			AddClonedScreenMesssage( nodeMove_Nodes.Count );
			//		}
			//	}
			//}
			//if( nodeMove_Activated )
			//{
			//	Vec2 mouseInUnits = ConvertScreenToUnit( viewport.MousePosition, true );
			//	Vec2 offsetInUnits = mouseInUnits - nodeMove_StartMousePositionInUnits;
			//	if( offsetInUnits.X < 0 )
			//		offsetInUnits.X -= .5f;
			//	if( offsetInUnits.Y < 0 )
			//		offsetInUnits.Y -= .5f;

			//	foreach( var node in nodeMove_Nodes )
			//		node.NodePosition = nodeMove_StartPositions[ node ] + offsetInUnits.ToVec2I();
			//}

			//scrolling view
			if( scrollView_Enabled )
			{
				Vector2I mouseInPixels = ( mouse * viewport.SizeInPixels.ToVector2() ).ToVector2I();
				Vector2I diff = scrollView_StartMousePositionInPixels - mouseInPixels;
				if( Math.Abs( diff.X ) > 2 || Math.Abs( diff.Y ) > 2 )
					scrollView_Activated = true;
			}
			if( scrollView_Activated )
			{
				Vector2 mouseDiff = mouse - scrollView_StartMousePositionInScreen;
				EditorExampleCanvas.EditorScrollPosition = scrollView_StartScrollPosition - ConvertScreenToUnit( mouseDiff, false );
			}

			//update select by rectangle
			if( selectByRectangle_Enabled )
			{
				Vector2 diffPixels = ( viewport.MousePosition - selectByRectangle_StartPosInScreen ) * viewport.SizeInPixels.ToVector2();
				if( Math.Abs( diffPixels.X ) >= 3 || Math.Abs( diffPixels.Y ) >= 3 )
					selectByRectangle_Activated = true;

				selectByRectangle_LastMousePositionInUnits = ConvertScreenToUnit( viewport.MousePosition, true );
			}
		}

		protected override void Viewport_MouseRelativeModeChanged( Viewport viewport, ref bool handled )
		{
			base.Viewport_MouseRelativeModeChanged( viewport, ref handled );
		}

		protected override void Viewport_MouseWheel( Viewport viewport, int delta, ref bool handled )
		{
			base.Viewport_MouseWheel( viewport, delta, ref handled );
			if( handled )
				return;

			//!!!!handled

			Vector2 mouse = viewport.MousePosition;
			Vector2 oldUnitsOnScreen = ConvertScreenToUnit( new Vector2( 1, 1 ), false );

			//int delta = e.Delta;
			bool updated = false;

			if( delta > 0 )
			{
				int steps = delta / 120;
				if( steps == 0 )
					steps = 1;

				for( int n = 0; n < steps; n++ )
				{
					if( EditorExampleCanvas.EditorZoomIndex < zoomTable.Length - 1 )
					{
						EditorExampleCanvas.EditorZoomIndex++;
						updated = true;
					}
				}
			}
			else if( delta < 0 )
			{
				int steps = -delta / 120;
				if( steps == 0 )
					steps = 1;

				for( int n = 0; n < steps; n++ )
				{
					if( EditorExampleCanvas.EditorZoomIndex > 0 )
					{
						EditorExampleCanvas.EditorZoomIndex--;
						updated = true;
					}
				}
			}

			if( updated )
			{
				Vector2 newUnitsOnScreen = ConvertScreenToUnit( new Vector2( 1, 1 ), false );

				Vector2 oldUnitsOffsetToCursorPosition = oldUnitsOnScreen * ( mouse - new Vector2( 0.5, 0.5 ) );
				Vector2 newUnitsOffsetToCursorPosition = newUnitsOnScreen * ( mouse - new Vector2( 0.5, 0.5 ) );

				Vector2 v = EditorExampleCanvas.EditorScrollPosition;
				v += oldUnitsOffsetToCursorPosition;
				v -= newUnitsOffsetToCursorPosition;
				EditorExampleCanvas.EditorScrollPosition = v;

				AddScreenMessage( string.Format( "Zoom {0}", GetZoom() ) );
			}

			handled = true;
		}

		protected override void Viewport_Tick( Viewport viewport, float delta )
		{
			base.Viewport_Tick( viewport, delta );
		}

		protected override void Viewport_UpdateBegin( Viewport viewport )
		{
			base.Viewport_UpdateBegin( viewport );
		}

		//List<Component_FlowchartNode> GetNodesByRectangle( Rect rectInUnits )
		//{
		//	List<Component_FlowchartNode> result = new List<Component_FlowchartNode>();

		//	foreach( Component_FlowchartNode node in EditorExampleCanvas.GetComponents<Component_FlowchartNode>( false ) )
		//	{
		//		var style = node.GetResultStyle( EditorExampleCanvas );
		//		if( style.IsIntersectsWithRectangle( this, node, rectInUnits ) )
		//			result.Add( node );
		//	}

		//	return result;
		//}

		//ESet<Component_FlowchartNode> SelectByRectangle_GetNodes()
		//{
		//	ESet<Component_FlowchartNode> result = new ESet<Component_FlowchartNode>();
		//	if( selectByRectangle_Activated )
		//	{
		//		foreach( var node in GetNodesByRectangle( SelectByRectangle_GetRectangleInUnits() ) )
		//			result.Add( node );
		//	}
		//	return result;
		//}

		bool CanSelectObjects()
		{
			//if( nodeMove_Activated )
			//	return false;
			return true;
		}

		protected override void Viewport_UpdateBeforeOutput( Viewport viewport )
		{
			base.Viewport_UpdateBeforeOutput( viewport );

			var renderer = viewport.CanvasRenderer;

			//UpdateFontSize( renderer );

			var visibleCells = GetVisibleCells();
			var mouseOverObject = GetMouseOverObject();

			//render background
			RenderBackgroundGrid();

			//display circle
			if( EditorExampleCanvas.DisplayCircle )
			{
				//get gabarites
				var objectRect = new RectangleI( -5, -5, 5, 5 );

				//check visibility
				var rectWithOneBorder = new RectangleI( objectRect.LeftTop - new Vector2I( 1, 1 ), objectRect.RightBottom + new Vector2I( 1, 1 ) );
				if( visibleCells.Intersects( rectWithOneBorder ) )
				{
					//visualization
					var screenRect = ConvertUnitToScreen( objectRect.ToRectangle() );
					renderer.AddFillEllipse( screenRect, 32, new ColorValue( 0, 160.0 / 255, 227.0 / 255, 1 ) );
				}
			}

			{
				renderer.AddRectangle( new Rectangle( 0.8, 0.03, 0.97, 0.047 ), new ColorValue( 1, 1, 0 ) );

				AddTextWithShadow( "Text drawing", new Vector2( 0.97, 0.05 ), EHorizontalAlignment.Right, EVerticalAlignment.Top, new ColorValue( 1, 1, 0 ) );
			}

			//draw selection rectangle
			if( selectByRectangle_Enabled && selectByRectangle_Activated )
			{
				Rectangle rect = new Rectangle( ConvertUnitToScreen( selectByRectangle_StartPosInUnits ) );
				rect.Add( viewport.MousePosition );

				Vector2I windowSize = viewport.SizeInPixels;
				Vector2 thickness = new Vector2( 1.0f / (float)windowSize.X, 1.0f / (float)windowSize.Y );

				renderer.AddRectangle( rect + thickness, new ColorValue( 0, 0, 0, .5f ) );
				renderer.AddRectangle( rect, new ColorValue( 0, 1, 0, 1 ) );
			}
		}

		protected override void Viewport_UpdateEnd( Viewport viewport )
		{
			base.Viewport_UpdateEnd( viewport );
		}

		//[Browsable( false )]
		//public EngineFont NodeFont
		//{
		//	get { return nodeFont; }
		//}

		public override void EditorActionGetState( EditorAction.GetStateContext context )
		{
			base.EditorActionGetState( context );
		}

		public override void EditorActionClick( EditorAction.ClickContext context )
		{
			base.EditorActionClick( context );
		}

		public bool CanNewObject( out List<Component> parentsForNewObjects )
		{
			parentsForNewObjects = new List<Component>();

			//foreach( var obj in SelectedObjects )
			//{
			//	var component = obj as Component;
			//	if( component != null )
			//		parentsForNewObjects.Add( component );
			//}

			//can create without selected objects?
			//return true;
			return parentsForNewObjects.Count != 0;
		}

		public void TryNewObject()
		{
			if( !CanNewObject( out List<Component> parentsForNewObjects ) )
				return;

			object parent;
			Vector2I position = Vector2I.Zero;
			if( parentsForNewObjects.Count != 0 )
			{
				//!!!!multiselection
				parent = parentsForNewObjects[ 0 ];
			}
			else
			{
				parent = EditorExampleCanvas;
				position = ConvertScreenToUnit( ViewportControl.Viewport.MousePosition, true ).ToVector2I();
			}

			var data = new NewObjectWindow.CreationDataClass();
			data.initDocumentWindow = this;
			data.initParentObjects = new List<object>();

			//!!!!multiselection
			data.initParentObjects.Add( parent );

			EditorAPI.OpenNewObjectWindow( data );
		}

		//public override bool CanDeleteObjects( out List<object> resultObjectsToDelete )
		//{
		//	resultObjectsToDelete = new List<object>();

		//	//foreach( var obj in SelectedObjects )
		//	//{
		//	//	var component = obj as Component;
		//	//	if( component != null )
		//	//		resultObjectsToDelete.Add( component );
		//	//}

		//	if( resultObjectsToDelete.Count == 0 )
		//		return false;

		//	return true;
		//}

		//public override bool TryDeleteObjects()
		//{
		//	//!!!!mutliselection
		//	//!!!!!�������� ����������-���������. ��� ��� ���

		//	if( !CanDeleteObjects( out List<object> objectsToDelete ) )
		//		return false;

		//	string text;
		//	if( objectsToDelete.Count == 1 )
		//	{
		//		//!!!!
		//		string template = Translate( "Are you sure you want to delete \"{0}\"?" );
		//		//string template = ToolsLocalization.Translate( "ContentBrowser", "Are you sure you want to delete \"{0}\"?" );

		//		var name = objectsToDelete[ 0 ].ToString();
		//		text = string.Format( template, name );
		//	}
		//	else
		//	{
		//		//!!!!
		//		string template = Translate( "Are you sure you want to delete these {0} objects?" );
		//		//string template = ToolsLocalization.Translate( "ContentBrowser", "Are you sure you want to delete these {0} objects?" );
		//		text = string.Format( template, objectsToDelete.Count );
		//	}

		//	if( EditorMessageBox.ShowQuestion( text, MessageBoxButtons.YesNo ) == DialogResult.No )
		//		return false;

		//	SelectObjects( null );

		//	//!!!!���? ����� ��� ��������� ���������� ����������. �������� ���� ������� ������
		//	List<Component> componentsToDelete = new List<Component>();
		//	List<UndoActionPropertiesChange.Item> propertiesChange = new List<UndoActionPropertiesChange.Item>();

		//	foreach( var objectToDelete in objectsToDelete )
		//	{
		//		var component = objectToDelete as Component;
		//		if( component != null )
		//			componentsToDelete.Add( component );
		//	}

		//	UndoSystem.Action resultAction = null;
		//	if( componentsToDelete.Count != 0 && propertiesChange.Count != 0 )
		//	{
		//		UndoMultiAction multiAction = new UndoMultiAction();
		//		multiAction.Actions.Add( new UndoActionComponentCreateDelete( Document, componentsToDelete, false ) );
		//		multiAction.Actions.Add( new UndoActionPropertiesChange( propertiesChange.ToArray() ) );
		//		resultAction = multiAction;
		//	}
		//	else if( componentsToDelete.Count != 0 )
		//		resultAction = new UndoActionComponentCreateDelete( Document, componentsToDelete, false );
		//	else if( propertiesChange.Count != 0 )
		//		resultAction = new UndoActionPropertiesChange( propertiesChange.ToArray() );

		//	Document.UndoSystem.CommitAction( resultAction );
		//	//var action = new UndoActionComponentCreateDelete( this, objectsToDelete, false );
		//	//Document.UndoSystem.CommitAction( action );
		//	Document.Modified = true;

		//	return true;
		//}

		public override bool CanCloneObjects( out List<Component> resultObjectsToClone )
		{
			resultObjectsToClone = new List<Component>();

			//!!!!mutliselection

			//!!!!�������� ���� � ����� ������. ��� ��� ���

			//var objectToNodes = GetObjectToNodesDictionary();

			////!!!!��� �� transform tool �����?
			//foreach( var obj in SelectedObjects )
			//{
			//	var component = obj as Component;
			//	if( component != null )
			//	{
			//		if( objectToNodes.TryGetValue( component, out List<Component_FlowchartNode> nodes ) )
			//		{
			//			foreach( var node in nodes )
			//				resultObjectsToClone.Add( node );
			//		}
			//		else
			//		{
			//			if( component.Parent != null )
			//				resultObjectsToClone.Add( component );
			//		}
			//	}
			//}

			if( resultObjectsToClone.Count == 0 )
				return false;

			return true;
		}

		public override void TryCloneObjects()
		{
			//!!!!mutliselection
			//!!!!!�������� ����������-���������. ��� ��� ���

			if( !CanCloneObjects( out List<Component> objectsToClone ) )
				return;

			List<Component> newObjects = new List<Component>();
			foreach( var obj in objectsToClone )
			{
				var newObject = EditorUtility.CloneComponent( obj );
				newObjects.Add( newObject );
			}

			//select objects
			{
				List<object> selectObjects = new List<object>();
				//!!!!��� ��������?
				selectObjects.AddRange( newObjects );

				SelectObjects( selectObjects );
			}

			if( newObjects.Count == 0 )
				return;

			//add to undo with deletion
			var action = new UndoActionComponentCreateDelete( Document, newObjects, true );
			Document.UndoSystem.CommitAction( action );
			Document.Modified = true;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private void Component_EditorExampleCanvas_DocumentWindow_DragEnter( object sender, DragEventArgs e )
		{
			//DragDropObjectCreate( e );
		}

		private void Component_EditorExampleCanvas_DocumentWindow_DragOver( object sender, DragEventArgs e )
		{
			//e.Effect = DragDropEffects.None;

			////!!!!���� ���
			//ViewportControl?.PerformMouseMove();

			//DragDropObjectUpdate();
			//if( dragDropObject != null )
			//	e.Effect = DragDropEffects.Link;

			//DragDropSetReferenceData dragDropData = (DragDropSetReferenceData)e.Data.GetData( typeof( DragDropSetReferenceData ) );
			//if( dragDropData != null )
			//	dragDropSetReferenceData = dragDropData;
			//if( dragDropSetReferenceData != null && dragDropSetReferenceDataCanSet )
			//	e.Effect = DragDropEffects.Link;

			////!!!!���� ���
			//ViewportControl.TryRender();
		}

		private void Component_EditorExampleCanvas_DocumentWindow_DragLeave( object sender, EventArgs e )
		{
			//DragDropObjectDestroy();

			//dragDropSetReferenceData = null;

			////!!!!���� ���
			//ViewportControl.TryRender();
		}

		private void Component_EditorExampleCanvas_DocumentWindow_DragDrop( object sender, DragEventArgs e )
		{
			//DragDropObjectCommit();

			//if( dragDropSetReferenceData != null )
			//{
			//	if( dragDropSetReferenceDataCanSet )
			//	{
			//		dragDropSetReferenceData.SetProperty( dragDropSetReferenceDataCanSetReferenceValues );
			//		dragDropSetReferenceDataCanSet = false;
			//	}
			//	dragDropSetReferenceData = null;
			//}
		}

		//void DragDropObjectCreate( DragEventArgs e )
		//{
		//	Metadata.TypeInfo createComponentType = null;
		//	string memberFullSignature = "";
		//	Component createNodeWithComponent = null;
		//	{
		//		var dragDropData = (ContentBrowser.DragDropData)e.Data.GetData( typeof( ContentBrowser.DragDropData ) );
		//		if( dragDropData != null )
		//		{
		//			var item = dragDropData.item;

		//			//!!!!�� ��� ����� ����� �������.

		//			//_File
		//			var fileItem = item as ContentBrowserItem_File;
		//			if( fileItem != null && !fileItem.IsDirectory )
		//			{
		//				//!!!!�� ������ ������������ ��� �����, �.�. �����. ��� ���?
		//				var ext = Path.GetExtension( fileItem.FullPath );
		//				if( ResourceManager.GetTypeByFileExtension( ext ) != null )
		//				{
		//					var res = ResourceManager.GetByName( VirtualPathUtils.GetVirtualPathByReal( fileItem.FullPath ) );

		//					var type = res?.PrimaryInstance?.ResultComponent?.GetProvidedType();
		//					if( type != null )
		//						createComponentType = type;
		//				}
		//			}

		//			//_Type
		//			var typeItem = item as ContentBrowserItem_Type;
		//			if( typeItem != null )
		//			{
		//				var type = typeItem.Type;

		//				//!!!!��� ���� ��� �������

		//				//!!!!������������ ����� ������� ����

		//				if( MetadataManager.GetTypeOfNetType( typeof( Component ) ).IsAssignableFrom( type ) && !type.Abstract )
		//					createComponentType = type;
		//			}

		//			//_Member
		//			var memberItem = item as ContentBrowserItem_Member;
		//			if( memberItem != null )
		//			{
		//				var member = memberItem.Member;

		//				//!!!!���?

		//				var type = member.Owner as Metadata.TypeInfo;
		//				if( type != null )
		//					memberFullSignature = string.Format( "{0}|{1}", type.Name, member.Signature );

		//				//!!!!���� �� �� �������?

		//				var component = member.Owner as Component;
		//				if( component != null )
		//					memberFullSignature = ReferenceUtils.CalculateResourceReference( component, member.Signature );
		//			}

		//			//_Component
		//			var componentItem = item as ContentBrowserItem_Component;
		//			if( componentItem != null )
		//			{
		//				var component = componentItem.Component;

		//				if( EditorExampleCanvas.ParentRoot == component.ParentRoot )
		//				{
		//					//add node with component
		//					createNodeWithComponent = component;
		//				}
		//				else
		//				{
		//					var resourceInstance = component.ParentRoot?.HierarchyController.CreatedByResource;
		//					if( resourceInstance != null )
		//					{
		//						//create component of type
		//						createComponentType = component.GetProvidedType();
		//					}
		//				}
		//			}
		//		}
		//	}

		//	if( createComponentType != null || memberFullSignature != "" || createNodeWithComponent != null )
		//	{
		//		//start creation

		//		//create node
		//		var node = EditorExampleCanvas.CreateComponent<Component_FlowchartNode>( -1, false );

		//		Component controlledObject = null;

		//		//specialization of the flowchart
		//		bool specializationHandled = false;
		//		var specialization = EditorExampleCanvas.Specialization.Value;
		//		if( specialization != null )
		//		{
		//			var context = new Component_FlowchartSpecialization.DragDropObjectCreateInitNodeContext();
		//			context.createComponentType = createComponentType;
		//			context.memberFullSignature = memberFullSignature;
		//			context.createNodeWithComponent = createNodeWithComponent;

		//			specialization.DragDropObjectCreateInitNode( node, context, ref specializationHandled );

		//			if( specializationHandled )
		//				controlledObject = context.controlledObject;
		//		}

		//		//default behaviour
		//		if( !specializationHandled )
		//		{
		//			//create component
		//			if( createComponentType != null && createComponentType != MetadataManager.GetTypeOfNetType( typeof( Component_FlowchartNode ) ) )
		//			{
		//				var obj = node.CreateComponent( createComponentType );

		//				//!!!!��� ���?

		//				var name = createComponentType.GetUserFriendlyNameForInstance();

		//				//!!!!��� �� ��� ��� ��������� �����

		//				{
		//					//!!!!���-�� ������� ����� ��� ����
		//					char[] invalidCharacters = new char[] { '/', '\\', ':' };
		//					foreach( var c in invalidCharacters )
		//						name = name.Replace( c, '_' );
		//				}

		//				obj.Name = name;

		//				obj.NewObjectSetDefaultConfiguration();

		//				controlledObject = obj;
		//			}

		//			//Component_InvokeMember
		//			if( memberFullSignature != "" )
		//			{
		//				var obj = node.CreateComponent<Component_InvokeMember>();
		//				obj.Name = "Invoke Member";
		//				obj.Member = new Reference<ReferenceValueType_Member>( null, memberFullSignature );
		//				controlledObject = obj;
		//			}

		//			//reference to object directly without creation child
		//			if( createNodeWithComponent != null )
		//				controlledObject = createNodeWithComponent;
		//		}

		//		//if( !window.ApplyCreationSettingsToObject( c ) )
		//		//	return false;

		//		//configure node with created object inside
		//		if( controlledObject != null )
		//		{
		//			var prefix = "Node " + controlledObject.Name + " ";
		//			node.Name = EditorExampleCanvas.Components.GetUniqueName( prefix, false, 1 );

		//			//set ControlledObject of the node
		//			node.ControlledObject = new Reference<Component>( null, ReferenceUtils.CalculateThisReference( node, controlledObject ) );
		//		}
		//		node.Enabled = true;
		//		dragDropObject = node;

		//		DragDropObjectUpdate();
		//	}
		//}

		//void DragDropObjectDestroy()
		//{
		//	if( dragDropObject != null )
		//	{
		//		dragDropObject.RemoveFromParent( true );
		//		dragDropObject.Dispose();
		//		dragDropObject = null;
		//	}
		//}

		//void DragDropObjectUpdate()
		//{
		//	if( dragDropObject != null )
		//	{
		//		var node = dragDropObject as Component_FlowchartNode;
		//		if( node != null )
		//		{
		//			var viewport = ViewportControl.Viewport;
		//			Vec2 mouse = viewport.MousePosition;

		//			//!!!!��������� ������� �� �����
		//			var positionInUnits = ConvertScreenToUnit( mouse, true ) - node.GetRepresentation().Size.ToVec2() / 2 + new Vec2( 1, 1 );
		//			node.NodePosition = positionInUnits.ToVec2I();
		//		}
		//	}
		//}

		//void DragDropObjectCommit()
		//{
		//	if( dragDropObject != null )
		//	{
		//		//add to undo with deletion
		//		var newObjects = new List<Component>();
		//		newObjects.Add( dragDropObject );
		//		var action = new UndoActionComponentCreateDelete( this, newObjects, true );
		//		Document.UndoSystem.CommitAction( action );
		//		Document.Modified = true;

		//		dragDropObject = null;

		//		EditorForm.Instance.SelectDockWindow( this );

		//		//!!!!�������� �������� ControlledObject
		//		//SelectObjects( newObjects.ToArray() );
		//	}
		//}

		void CalculateCellSize()
		{
			cellSize = 14;

			float dpi = EditorAPI.DPI;
			if( dpi > 96 )
			{
				cellSize *= dpi / 96;
				cellSize = (int)cellSize;
			}
		}

		void RenderBackgroundGrid()
		{
			var viewport = ViewportControl.Viewport;
			var renderer = viewport.CanvasRenderer;

			RectangleI visibleCells = GetVisibleCells();

			//draw background
			renderer.AddQuad( new Rectangle( 0, 0, 1, 1 ), new ColorValue( .17f, .17f, .17f ) );

			//draw grid
			if( GetZoom() > .5f )
			{
				var lines = new List<CanvasRenderer.LineItem>( 256 );

				{
					ColorValue color = new ColorValue( .2f, .2f, .2f );
					for( int x = visibleCells.Left; x <= visibleCells.Right; x++ )
					{
						if( x % 10 != 0 )
						{
							var floatX = (float)ConvertUnitToScreenX( x );
							lines.Add( new CanvasRenderer.LineItem( new Vector2F( floatX, 0 ), new Vector2F( floatX, 1 ), color ) );
						}
					}
					for( int y = visibleCells.Top; y <= visibleCells.Bottom; y++ )
					{
						if( y % 10 != 0 )
						{
							var floatY = (float)ConvertUnitToScreenY( y );
							lines.Add( new CanvasRenderer.LineItem( new Vector2F( 0, floatY ), new Vector2F( 1, floatY ), color ) );
						}
					}
				}

				{
					ColorValue color = new ColorValue( .1f, .1f, .1f );
					for( int x = visibleCells.Left; x <= visibleCells.Right; x++ )
					{
						if( x % 10 == 0 )
						{
							var floatX = (float)ConvertUnitToScreenX( x );
							lines.Add( new CanvasRenderer.LineItem( new Vector2F( floatX, 0 ), new Vector2F( floatX, 1 ), color ) );
						}
					}
					for( int y = visibleCells.Top; y <= visibleCells.Bottom; y++ )
					{
						if( y % 10 == 0 )
						{
							var floatY = (float)ConvertUnitToScreenY( y );
							lines.Add( new CanvasRenderer.LineItem( new Vector2F( 0, floatY ), new Vector2F( 1, floatY ), color ) );
						}
					}
				}

				viewport.CanvasRenderer.AddLines( lines );
			}
		}
	}
}
